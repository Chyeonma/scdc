"""Independent stdlib HMAC/UUID byte-order verifier for public synthetic vectors."""
import hashlib
import hmac
import json
import pathlib
import struct
import sys
import uuid

data = json.loads(pathlib.Path(sys.argv[1]).read_text(encoding="utf-8-sig"))
key = bytes.fromhex(data["syntheticKeyHex"])
prefix = b"SCDC.Send.v1\0" + b"".join(uuid.UUID(data[name]).bytes for name in ("spaceId", "authorId", "clientMessageId"))
results = []
for case in data["cases"]:
    content = case["inputContent"].replace("\r\n", "\n").replace("\r", "\n")
    body = content.encode("utf-8")
    digest = hmac.new(key, prefix + struct.pack(">I", len(body)) + body, hashlib.sha256).hexdigest()
    if content != case["normalizedContent"] or not hmac.compare_digest(digest, case["expectedFingerprintHex"]):
        raise RuntimeError("Synthetic fingerprint vector mismatch: " + case["id"])
    results.append({"id": case["id"], "result": "PASS"})
a, b = (uuid.UUID(value) for value in data["uuidOrderingPair"])
if not a.bytes < b.bytes or not a.bytes_le > b.bytes_le:
    raise RuntimeError("UUID fixture does not distinguish network and little endian order")
print(json.dumps({"vectors": results, "count": len(results), "uuidNetworkOrder": "PASS", "keyBytesPrinted": False}))
