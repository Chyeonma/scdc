[CmdletBinding()]
param([string]$ProofRun='p4proof')
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force -DisableNameChecking
Assert-DmDatabase
$paths=Get-DmPaths; $folder=Get-DmRunPath $ProofRun
$OutputEncoding=[Text.UTF8Encoding]::new($false)
$prefix='scdc_dm_p4_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$upgrade=$prefix+'_upgrade_test'; $fresh=$prefix+'_fresh_test'; $created=@()
function Sql([string]$Database,[string]$Query){
 if($Database -notin @('scdc_dm_acceptance_test',$upgrade,$fresh)){throw 'Unexpected database'}
 $lines=$Query | & docker compose --project-name scdc-dm-acceptance --env-file $paths.Env -f (Join-Path $paths.Repo 'compose.dm-test.yaml') exec -T postgres psql -X -qAt -U scdc_dm_test -d $Database -v ON_ERROR_STOP=1
 if($LASTEXITCODE -ne 0){throw 'Realtime migration proof failed'};return ($lines -join "`n")
}
$observe=@'
SELECT json_build_object('pending',count(*),'metadata',json_agg(json_build_object('id',id,'eventType',event_type,'aggregateType',aggregate_type,'aggregateId',aggregate_id,'spaceId',space_id,'payload',payload,'occurredAt',occurred_at,'availableAt',available_at,'publishedAt',published_at,'attempts',attempt_count,'lastError',last_error) ORDER BY id))::text FROM integration.outbox_events;
'@
$check=@'
SELECT json_build_object('leaseColumns',(SELECT count(*) FROM information_schema.columns WHERE table_schema='integration' AND table_name='outbox_events' AND column_name IN ('lease_token','lease_expires_at') AND is_nullable='YES'),'dispatchIndex',to_regclass('integration.ix_outbox_events_dispatch') IS NOT NULL,'leaseConstraint',EXISTS(SELECT 1 FROM pg_constraint WHERE conrelid='integration.outbox_events'::regclass AND conname='ck_outbox_events_lease'),'leased',(SELECT count(*) FROM integration.outbox_events WHERE lease_token IS NOT NULL OR lease_expires_at IS NOT NULL))::text;
'@
try{
 foreach($db in @($upgrade,$fresh)){$null=Sql scdc_dm_acceptance_test "CREATE DATABASE $db;"; $created+=$db}
 $base=& git -c "safe.directory=$($paths.Repo)" -C $paths.Repo show '9de920abda42fa26b15303a8705e61f612c4ff52:database/postgres/schema.sql'
 if($LASTEXITCODE -ne 0){throw 'Accepted schema cannot be read'}; $null=Sql $upgrade ($base -join "`n")
 $null=Sql $upgrade @'
INSERT INTO integration.outbox_events(id,event_type,aggregate_type,aggregate_id,aggregate_version,space_id,payload,occurred_at,available_at,attempt_count,last_error) VALUES
('00000004-0000-4000-8000-000000000001','Messaging.MessageChanged','Message','00000004-0000-4000-8000-000000000002',1,'00000004-0000-4000-8000-000000000003','{"messageId":"00000004-0000-4000-8000-000000000002","conversationId":"00000004-0000-4000-8000-000000000003","version":"1"}', '2026-10-01T00:00:00Z','2026-10-01T00:00:10Z',3,'DISPATCH_UNAVAILABLE');
'@
 $before=Sql $upgrade $observe
 $migration=Get-Content (Join-Path $paths.Repo 'database/postgres/migrations/20261010_dm_p4_outbox_lease.sql') -Raw -Encoding UTF8
 $null=Sql $upgrade $migration; $null=Sql $upgrade $migration; $after=Sql $upgrade $observe
 if($before -ne $after){throw 'Pending event metadata changed during replay'}
 $upgradeCheck=(Sql $upgrade $check)|ConvertFrom-Json
 $null=Sql $fresh (Get-Content (Join-Path $paths.Repo 'database/postgres/schema.sql') -Raw -Encoding UTF8)
 $null=Sql $fresh $migration; $null=Sql $fresh $migration; $freshCheck=(Sql $fresh $check)|ConvertFrom-Json
 foreach($result in @($upgradeCheck,$freshCheck)){if($result.leaseColumns -ne 2 -or !$result.dispatchIndex -or !$result.leaseConstraint -or $result.leased -ne 0){throw 'Lease schema invariant failed'}}
 $proof=@{acceptedBaseline='9de920abda42fa26b15303a8705e61f612c4ff52';pendingBefore=($before|ConvertFrom-Json);pendingAfter=($after|ConvertFrom-Json);upgrade=$upgradeCheck;fresh=$freshCheck;replaysPerDatabase=2;result='PASS'}
 Write-DmJson (Join-Path $folder 'realtime-migration.json') $proof; $proof|ConvertTo-Json -Depth 10
}finally{
 foreach($db in $created){if($db -notmatch '^scdc_dm_p4_[a-f0-9]{8}_(upgrade|fresh)_test$'){throw 'Unsafe cleanup target'}; $null=Sql scdc_dm_acceptance_test "DROP DATABASE $db;"}
}
