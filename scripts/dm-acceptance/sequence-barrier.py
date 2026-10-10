"""Local acceptance process: one owned advisory gate, no HTTP control endpoint."""
import json
import os
import pathlib
import subprocess
import sys
import time

config_path = pathlib.Path(sys.argv[1]).resolve()
config = json.loads(config_path.read_text(encoding="utf-8-sig"))
folder = config_path.parent
# Own the helper's streams. A detached hidden process must not retain a caller's pipe.
sys.stdin.close()
sys.stdin = open(os.devnull, encoding="utf-8")
sys.stdout.close()
sys.stdout = (folder / "stdout.log").open("w", encoding="utf-8")
sys.stderr.close()
sys.stderr = (folder / "stderr.log").open("w", encoding="utf-8")
state_path = folder / "barrier-state.json"
control_path = folder / "barrier-release.txt"

def state(**values):
    data = {"run": config["run"], "conversationId": config["space"], "operation": config["operation"], **values}
    temporary = folder / "barrier-state.tmp"
    temporary.write_text(json.dumps(data), encoding="utf-8")
    temporary.replace(state_path)

command = ["docker", "compose", "--project-name", "scdc-dm-acceptance", "--env-file", config["env"],
           "-f", config["compose"], "exec", "-T", "postgres", "psql", "-X", "-qAt",
           "-U", "scdc_dm_test", "-d", "scdc_dm_acceptance_test", "-v", "ON_ERROR_STOP=1"]
process = subprocess.Popen(command, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                           text=True, encoding="utf-8", bufsize=1)
def sql(statement, marker):
    process.stdin.write(statement + f"\nSELECT '{marker}';\n")
    process.stdin.flush()
    lines = []
    while True:
        line = process.stdout.readline()
        if not line:
            raise RuntimeError("Acceptance SQL process exited; SQL and stderr suppressed.")
        if line.strip() == marker:
            return lines
        if line.strip():
            lines.append(line.strip())

trigger = config["trigger"]
gate = int(config["gate"])
space = str(__import__('uuid').UUID(config["space"]))
actor = str(__import__('uuid').UUID(config["actor"]))
operation = str(__import__('uuid').UUID(config["operation"]))
if not trigger.startswith("dm_p3_gate_") or not trigger[11:].isalnum():
    raise ValueError("Invalid trigger name")
rollback = "RAISE EXCEPTION USING ERRCODE='58000',MESSAGE='Owned sequence rollback';" if config["rollback"] else ""
installed = False
try:
    pid = int(sql(f"SELECT pg_backend_pid(); SELECT pg_advisory_lock({gate});", "GATE_HELD")[0])
    sql(f"""
DO $$ BEGIN IF current_database()<>'scdc_dm_acceptance_test' OR NOT EXISTS(
 SELECT 1 FROM messaging.spaces WHERE id='{space}' AND created_by_user_id='{actor}')
 THEN RAISE EXCEPTION 'Owned acceptance space required'; END IF; END $$;
CREATE FUNCTION integration.{trigger}() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF NEW.space_id='{space}' AND EXISTS(SELECT 1 FROM messaging.messages
 WHERE id=NEW.aggregate_id AND author_user_id='{actor}' AND client_message_id='{operation}') THEN
 PERFORM pg_advisory_xact_lock({gate}); {rollback}
 END IF; RETURN NEW;
END $$;
CREATE TRIGGER {trigger} BEFORE INSERT ON integration.outbox_events
 FOR EACH ROW EXECUTE FUNCTION integration.{trigger}();
""", "GATE_READY")
    installed = True
    state(held=True, gatePid=pid, gate=gate, trigger=trigger, rollback=config["rollback"])
    deadline = time.monotonic() + 120
    while time.monotonic() < deadline:
        if control_path.exists() and control_path.read_text(encoding="utf-8-sig").strip() == config["nonce"]:
            break
        time.sleep(0.05)
    reason = "released" if time.monotonic() < deadline else "expired-120s"
    sql(f"SELECT pg_advisory_unlock({gate}); SET lock_timeout='10s'; DROP TRIGGER {trigger} ON integration.outbox_events; DROP FUNCTION integration.{trigger}();", "GATE_CLEAN")
    installed = False
    state(held=False, gatePid=pid, gate=gate, trigger=trigger, reason=reason, cleaned=True)
except Exception as error:
    state(held=False, cleaned=False, error=type(error).__name__)
    raise
finally:
    if process.poll() is None:
        try:
            if installed:
                sql(f"SELECT pg_advisory_unlock({gate}); SET lock_timeout='10s'; DROP TRIGGER IF EXISTS {trigger} ON integration.outbox_events; DROP FUNCTION IF EXISTS integration.{trigger}();", "GATE_FINALLY")
            process.stdin.close()
            process.wait(timeout=15)
        except (OSError, RuntimeError, subprocess.TimeoutExpired):
            process.terminate()
