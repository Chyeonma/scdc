[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force -DisableNameChecking
Assert-DmDatabase
$paths=Get-DmPaths
$OutputEncoding=[System.Text.UTF8Encoding]::new($false)
$prefix='scdc_dm_p2_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$upgrade=$prefix+'_upgrade_test'
$fresh=$prefix+'_fresh_test'
$upgradeCreated=$false
$freshCreated=$false
function Invoke-ProofSql([string]$Database,[string]$Sql) {
 if ($Database -notin @('scdc_dm_acceptance_test',$upgrade,$fresh)) { throw 'Unexpected database.' }
 $lines=$Sql | & docker compose --project-name scdc-dm-acceptance --env-file $paths.Env -f (Join-Path $paths.Repo 'compose.dm-test.yaml') exec -T postgres psql -X -q -A -t -U scdc_dm_test -d $Database -v ON_ERROR_STOP=1
 if ($LASTEXITCODE -ne 0) { throw 'Migration proof failed.' }
 return ($lines -join "`n")
}
try {
 $null=Invoke-ProofSql scdc_dm_acceptance_test "CREATE DATABASE $upgrade;"
 $upgradeCreated=$true
 $null=Invoke-ProofSql scdc_dm_acceptance_test "CREATE DATABASE $fresh;"
 $freshCreated=$true
 $baseline=& git -c "safe.directory=$($paths.Repo)" -C $paths.Repo show '6e9ea045aaea5b87734265f0cbbb050c01842203:database/postgres/schema.sql'
 if ($LASTEXITCODE -ne 0) { throw 'Cannot read accepted P1 schema.' }
 $null=Invoke-ProofSql $upgrade ($baseline -join "`n")
 $null=Invoke-ProofSql $upgrade @'
BEGIN;
INSERT INTO identity.users(id,username,status) VALUES
 ('00000001-0000-4000-8000-000000000001','migration_an',1),('00000001-0000-4000-8000-000000000002','migration_bao',1);
INSERT INTO messaging.spaces(id,space_type,created_by_user_id,last_message_sequence) VALUES
 ('00000001-0000-4000-8000-000000000010',1,'00000001-0000-4000-8000-000000000001',42);
INSERT INTO messaging.direct_conversations(space_id,user_low_id,user_high_id) VALUES
 ('00000001-0000-4000-8000-000000000010','00000001-0000-4000-8000-000000000001','00000001-0000-4000-8000-000000000002');
INSERT INTO messaging.space_members(space_id,user_id) VALUES
 ('00000001-0000-4000-8000-000000000010','00000001-0000-4000-8000-000000000001'),
 ('00000001-0000-4000-8000-000000000010','00000001-0000-4000-8000-000000000002');
INSERT INTO messaging.messages(space_id,author_user_id,client_message_id,content) VALUES
 ('00000001-0000-4000-8000-000000000010','00000001-0000-4000-8000-000000000001','00000001-0000-4000-8000-000000000031','edited legacy body'),
 ('00000001-0000-4000-8000-000000000010','00000001-0000-4000-8000-000000000002','00000001-0000-4000-8000-000000000032','legacy second body');
COMMIT;
'@
 $migration=Get-Content (Join-Path $paths.Repo 'database/postgres/migrations/20261009_dm_p2_text_message.sql') -Raw -Encoding UTF8
 $null=Invoke-ProofSql $upgrade $migration
 $null=Invoke-ProofSql $upgrade $migration
 $upgradeProof=Invoke-ProofSql $upgrade @'
SELECT json_build_object('legacyMessages',(SELECT count(*) FROM messaging.messages),
 'sequences',(SELECT json_agg(conversation_sequence::text ORDER BY sequence_no) FROM messaging.messages),
 'retainedCounter',(SELECT last_message_sequence::text FROM messaging.spaces),
 'unverifiableOperations',(SELECT count(*) FROM messaging.send_operations WHERE fingerprint IS NULL AND fingerprint_version IS NULL AND key_id IS NULL),
 'oldBodyCopies',(SELECT count(*) FROM messaging.message_edits),'outboxRows',(SELECT count(*) FROM integration.outbox_events))::text;
'@
 $proof=$upgradeProof | ConvertFrom-Json
 if ($proof.legacyMessages -ne 2 -or ($proof.sequences -join ',') -ne '1,2' -or $proof.retainedCounter -ne '42' -or $proof.unverifiableOperations -ne 2 -or $proof.oldBodyCopies -ne 0 -or $proof.outboxRows -ne 0) { throw 'Legacy migration invariant failed.' }
 $null=Invoke-ProofSql $upgrade "UPDATE messaging.messages SET content=NULL,deleted_at=created_at WHERE client_message_id='00000001-0000-4000-8000-000000000031';"
 $null=Invoke-ProofSql $fresh (Get-Content (Join-Path $paths.Repo 'database/postgres/schema.sql') -Raw -Encoding UTF8)
 $null=Invoke-ProofSql $fresh $migration
 $freshProof=Invoke-ProofSql $fresh "SELECT json_build_object('conversationSequencePresent',EXISTS(SELECT 1 FROM information_schema.columns WHERE table_schema='messaging' AND table_name='messages' AND column_name='conversation_sequence' AND is_nullable='NO'),'sendOperationsPresent',to_regclass('messaging.send_operations') IS NOT NULL,'messageCount',(SELECT count(*) FROM messaging.messages))::text;"
 $current=$freshProof | ConvertFrom-Json
 if (-not $current.conversationSequencePresent -or -not $current.sendOperationsPresent -or $current.messageCount -ne 0) { throw 'Fresh schema invariant failed.' }
 $evidence=[pscustomobject]@{acceptedBaseline='6e9ea045aaea5b87734265f0cbbb050c01842203';upgrade=$proof;fresh=$current;tombstoneAccepted=$true;replayPassed=$true}
 Write-DmJson (Join-Path (Get-DmRunPath 'p2proof') 'migration.json') $evidence
 $evidence | ConvertTo-Json -Depth 8
} finally {
 # Only the exact two new, randomly named proof databases are disposable.
 if ($upgrade -notmatch '^scdc_dm_p2_[a-f0-9]{8}_upgrade_test$' -or $fresh -notmatch '^scdc_dm_p2_[a-f0-9]{8}_fresh_test$') { throw 'Unsafe cleanup target.' }
 if ($upgradeCreated) { $null=Invoke-ProofSql scdc_dm_acceptance_test "DROP DATABASE $upgrade;" }
 if ($freshCreated) { $null=Invoke-ProofSql scdc_dm_acceptance_test "DROP DATABASE $fresh;" }
}
