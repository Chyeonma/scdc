[CmdletBinding()]
param(
 [Parameter(Mandatory)][ValidateSet('Setup','Fixtures','Hold','Inspect','Release','Prove','Keys','KeyStatus','Legacy','Catchup','Snapshot','Fingerprint')][string]$Action,
 [string]$Run='p3sequser', [ValidateSet('C01','C02','C03','C04','R101')][string]$Case='C01',
 [ValidateSet('K1','K2','MissingK1','MissingK2','Restore')][string]$KeyMode='K1', [guid]$MessageId=[guid]::Empty,
 [ValidateRange(0,5)][int]$ExpectedWaiters=0
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$OutputEncoding=[System.Text.UTF8Encoding]::new($false)
Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force -DisableNameChecking
Assert-DmDatabase
$paths=Get-DmPaths; $folder=Get-DmRunPath $Run
if($Run -eq 'baseline'){throw 'Use a dedicated concurrency run.'}
if($Action -eq 'Fingerprint'){
 $vectorOutput= & python (Join-Path $PSScriptRoot 'fingerprint-proof.py') (Join-Path $paths.Repo 'docs/fixtures/dm-fingerprint.json')
 if($LASTEXITCODE -ne 0){throw 'Fingerprint vectors failed.'}
 $pairCheck=Invoke-DmReadSql "SELECT ('00000001-0000-4000-8000-000000000000'::uuid < '00000100-0000-4000-8000-000000000000'::uuid)::text;"
 if($pairCheck.Trim() -ne 'true'){throw 'Postgres UUID fixture ordering mismatch.'}
 $proof=@{vectors=($vectorOutput | ConvertFrom-Json);postgresExactUuidPairOrder=$true;run=$Run;result='PASS'}
 Write-DmJson (Join-Path $folder 'fingerprint-proof.json') $proof
 $proof | ConvertTo-Json -Depth 10; exit
}
$fixtureFile=Join-Path $folder 'sequence-fixtures.json'
function Sql([string]$Query) {
 $Query | & docker compose --project-name scdc-dm-acceptance --env-file $paths.Env -f (Join-Path $paths.Repo 'compose.dm-test.yaml') exec -T postgres psql -X -qAt -U scdc_dm_test -d scdc_dm_acceptance_test -v ON_ERROR_STOP=1
 if($LASTEXITCODE -ne 0){throw 'Acceptance SQL failed.'}
}
function Logout($Session){$null=Invoke-DmRequest POST /auth/logout @{refreshToken=$Session.refreshToken}}
function Ready {
 for($i=0;$i -lt 40;$i++) {
  try { if((Invoke-DmRequest GET /health).Status -eq 200 -and (Invoke-DmRequest GET /health -ViaWeb).Status -eq 200){Assert-DmApiTarget;return} } catch {}
  Start-Sleep -Milliseconds 500
 }; throw 'API/proxy not ready.'
}
if($Action -eq 'Setup'){
 Assert-DmApiTarget
 if(Test-Path (Join-Path $folder 'manifest.json')){throw 'Run exists; no reset performed.'}
 Set-DmFixtures $Run; exit
}
$manifest=Get-DmManifest $Run
$a=@($manifest.accounts | Where-Object alias -eq A)[0]
if($Action -eq 'Fixtures'){
 Assert-DmApiTarget
 if(Test-Path $fixtureFile){throw 'Fixture already exists. Reuse IDs or choose a new run.'}
 $session=Connect-DmActor A $Run 'DM-P3-T02-fixtures'
 try {
  $pairs=@()
  foreach($lane in @(@{case='C01';peer='B'},@{case='C02';peer='C'},@{case='C03';peer='S01'},@{case='C04';peer='S02'},@{case='R101';peer='S03'})){
   $peer=@($manifest.accounts | Where-Object alias -eq $lane.peer)[0]
   $opened=Invoke-DmRequest POST /direct-conversations @{peerUserId=$peer.id} -AccessToken $session.accessToken
   Assert-DmStatus $opened 200 'Open sequence pair'
   $pairs+=@([pscustomobject]@{case=$lane.case;peerAlias=$lane.peer;peerUsername=$peer.username;conversationId=$opened.Body.id;X=[guid]::NewGuid().ToString();Y=[guid]::NewGuid().ToString()})
  }
  $data=@{run=$Run;actorId=$a.id;actorUsername=$a.username;pairs=$pairs}
  Write-DmJson $fixtureFile $data; $data | ConvertTo-Json -Depth 7
 } finally {Logout $session}; exit
}
$fixtures=Get-Content -LiteralPath $fixtureFile -Raw -Encoding UTF8 | ConvertFrom-Json
$lane=@($fixtures.pairs | Where-Object case -eq $Case)[0]
$space=([guid]$lane.conversationId).ToString(); $actor=([guid]$a.id).ToString()
if($Action -eq 'Snapshot'){
 & (Join-Path $PSScriptRoot 'send-text.ps1') -Action Snapshot -Run $Run -PeerAlias $lane.peerAlias; exit
}
if($Action -eq 'Prove'){
 if($Case -notin @('C01','C02')){throw 'Prove only supports commit/rollback lanes.'}
 Assert-DmApiTarget
 $before= & (Join-Path $PSScriptRoot 'send-text.ps1') -Action Snapshot -Run $Run -PeerAlias $lane.peerAlias | ConvertFrom-Json
 $a1=Connect-DmActor A $Run 'DM-P3-T02-A1'; $a2=Connect-DmActor A $Run 'DM-P3-T02-A2'
 $http=[Net.Http.HttpClient]::new(); $http.Timeout=[TimeSpan]::FromSeconds(60)
 $requests=@(); $responses=@(); $gateStarted=$false; $released=$false
 function Gate([string]$Kind,[int]$Waiters=0){
  $output= & powershell -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -Action $Kind -Run $Run -Case $Case -ExpectedWaiters $Waiters
  if($LASTEXITCODE -ne 0){throw "Gate $Kind failed"}; return ($output | ConvertFrom-Json)
 }
 try {
  $baseline=Invoke-DmRequest GET "/direct-conversations/$space/messages?limit=50" -AccessToken $a1.accessToken
  Assert-DmStatus $baseline 200 'Baseline resume'
  $hold=Gate Hold; $gateStarted=$true; $tasks=@()
  foreach($item in @(@{session=$a1;uuid=$lane.X;content=$(if($Case -eq 'C01'){'SEQ-X-C01'}else{'ROLLBACK-X-C02'})},@{session=$a2;uuid=$lane.Y;content=$(if($Case -eq 'C01'){'SEQ-Y-C01'}else{'COMMIT-Y-C02'})})){
   $req=[Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::Post,(Get-DmUrls).Api+"/direct-conversations/$space/messages")
   $req.Headers.Authorization=[Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer',$item.session.accessToken)
   $req.Content=[Net.Http.StringContent]::new((@{clientMessageId=$item.uuid;content=$item.content}|ConvertTo-Json),[Text.Encoding]::UTF8,'application/json')
   $requests+=@($req); $tasks+=@($http.SendAsync($req)); $null=Gate Inspect $tasks.Count
  }
  $committedProbe= & (Join-Path $PSScriptRoot 'send-text.ps1') -Action Snapshot -Run $Run -PeerAlias $lane.peerAlias | ConvertFrom-Json
  if(($committedProbe | ConvertTo-Json -Compress -Depth 10) -cne ($before | ConvertTo-Json -Compress -Depth 10)){throw 'Uncommitted writer became visible in MVCC probe.'}
  $null=Gate Release; $released=$true; $statuses=@(); $ids=@(); $sequences=@()
  for($i=0;$i -lt 2;$i++){
   $response=$tasks[$i].GetAwaiter().GetResult(); $responses+=@($response); $statuses+=@([int]$response.StatusCode)
   $dto=$response.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
   if($Case -eq 'C02' -and $i -eq 0){if([int]$response.StatusCode -ne 503 -or $dto.errorCode -ne 'AUTHORITY_UNAVAILABLE'){throw 'Rollback status mismatch'}}
   else{
    if([int]$response.StatusCode -ne 200){throw 'Writer did not commit200'}
    $expectedUuid=$lane.X; if($i -eq 1){$expectedUuid=$lane.Y}
    $increment=$i+1; if($Case -eq 'C02'){$increment=1}
    if($dto.clientMessageId -ne $expectedUuid -or $dto.sequence -ne ([long]$before.lastSequence+$increment).ToString()){throw 'Writer UUID/sequence mismatch'}
    $ids+=@($dto.id); $sequences+=@($dto.sequence)
   }
  }
  $after= & (Join-Path $PSScriptRoot 'send-text.ps1') -Action Snapshot -Run $Run -PeerAlias $lane.peerAlias | ConvertFrom-Json
  $expected=2; if($Case -eq 'C02'){$expected=1}
  foreach($field in @('messageCount','operationCount','outboxCount')){if($after.$field-$before.$field -ne $expected){throw 'Record delta mismatch'}}
  if([long]$after.lastSequence-[long]$before.lastSequence -ne $expected){throw 'Counter mismatch'}
  $history=Invoke-DmRequest GET ("/direct-conversations/$space/messages?limit=50&after="+[uri]::EscapeDataString($baseline.Body.resumeCursor)) -AccessToken $a1.accessToken
  Assert-DmStatus $history 200 'Committed history'
  if(($ids -join ',') -cne (@($history.Body.items | ForEach-Object id) -join ',')){throw 'History writer union mismatch'}
  $proof=@{run=$Run;case=$Case;conversationId=$space;statuses=$statuses;ids=$ids;sequences=$sequences;before=$before;uncommittedProbe=$committedProbe;after=$after;barrier=$hold;independentSessions=$true;result='PASS'}
  Write-DmJson (Join-Path $folder ('sequence-be-'+$Case+'.json')) $proof; $proof | ConvertTo-Json -Depth 10
 }finally{
  if($gateStarted -and !$released){$null=Gate Release}
  foreach($response in $responses){$response.Dispose()}; foreach($req in $requests){$req.Dispose()}; $http.Dispose(); Logout $a1; Logout $a2
 }; exit
}
if($Action -in @('Hold','Inspect','Release')){
 $gateFolder=Join-Path $folder ('barrier-'+$Case); $null=New-Item -ItemType Directory -Path $gateFolder -Force
 $stateFile=Join-Path $gateFolder 'barrier-state.json'; $configFile=Join-Path $gateFolder 'barrier-config.json'
 if($Action -eq 'Hold'){
  Assert-DmApiTarget
  if(Test-Path $stateFile){$previous=Get-Content $stateFile -Raw | ConvertFrom-Json; if($previous.held){throw 'Gate already held; Release first.'}}
  $count=(Invoke-DmReadSql "SELECT count(*) FROM messaging.messages WHERE space_id='$space' AND client_message_id='$($lane.X)';").Trim()
  if($count -ne '0'){throw 'X already committed; use a new run for a fresh barrier.'}
  $config=@{run=$Run;space=$space;actor=$actor;operation=$lane.X;rollback=($Case -eq 'C02');gate=Get-Random -Minimum 1000000000 -Maximum 2000000000;trigger=('dm_p3_gate_'+[guid]::NewGuid().ToString('N'));nonce=[guid]::NewGuid().ToString();env=$paths.Env;compose=(Join-Path $paths.Repo 'compose.dm-test.yaml')}
  Write-DmJson $configFile $config
  if(Test-Path $stateFile){Remove-Item -LiteralPath $stateFile}
  $python=Get-Command python.exe -ErrorAction Stop
  $scriptPath=Join-Path $PSScriptRoot 'sequence-barrier.py'
  $launchArgs=@('-u',('"'+$scriptPath+'"'),('"'+$configFile+'"'))
  $process=Start-Process -FilePath $python.Source -ArgumentList $launchArgs -WindowStyle Hidden -PassThru
  for($i=0;$i -lt 100;$i++){
   if(Test-Path $stateFile){$gateState=Get-Content $stateFile -Raw | ConvertFrom-Json; if(!$gateState.held){throw 'Gate failed; see scoped stderr log.'}; $gateState | ConvertTo-Json; exit}
   if($process.HasExited){throw 'Gate process exited before ready.'}; Start-Sleep -Milliseconds 100
  }; throw 'Gate startup timeout; process has a120s release deadline.'
 }
 if(!(Test-Path $stateFile)){throw 'Hold first.'}
 $gateState=Get-Content $stateFile -Raw | ConvertFrom-Json
 if($Action -eq 'Release'){
  $config=Get-Content $configFile -Raw | ConvertFrom-Json
  [IO.File]::WriteAllText((Join-Path $gateFolder 'barrier-release.txt'),$config.nonce,[Text.UTF8Encoding]::new($false))
  for($i=0;$i -lt 150;$i++){
   $gateState=Get-Content $stateFile -Raw | ConvertFrom-Json
   if(!$gateState.held){if(!$gateState.cleaned){throw 'Gate cleanup failed'}; $gateState | ConvertTo-Json; exit}; Start-Sleep -Milliseconds 100
  }
  # A terminal/browser runner can be interrupted and orphan its detached helper.
  # Terminate only this exact advisory-lock owner in the acceptance DB, then remove its random trigger.
  $trigger=[string]$gateState.trigger; $ownerPid=[int]$gateState.gatePid; $gateKey=[long]$gateState.gate
  if($trigger -notmatch '^dm_p3_gate_[a-f0-9]{32}$' -or $gateKey -lt 1000000000 -or $gateKey -ge 2000000000){throw 'Unsafe orphan cleanup metadata'}
  $null=Sql "SELECT pg_terminate_backend(a.pid) FROM pg_stat_activity a WHERE a.pid=$ownerPid AND a.datname='scdc_dm_acceptance_test' AND a.application_name='psql' AND EXISTS(SELECT 1 FROM pg_locks l WHERE l.pid=a.pid AND l.locktype='advisory' AND l.granted AND l.classid=0 AND l.objid=$gateKey AND l.objsubid=1); SET lock_timeout='10s'; DROP TRIGGER IF EXISTS $trigger ON integration.outbox_events; DROP FUNCTION IF EXISTS integration.$trigger();"
  $gateState | Add-Member -NotePropertyName held -NotePropertyValue $false -Force
  $gateState | Add-Member -NotePropertyName cleaned -NotePropertyValue $true -Force
  $gateState | Add-Member -NotePropertyName reason -NotePropertyValue 'orphan-cleanup' -Force
  Write-DmJson $stateFile $gateState; $gateState | ConvertTo-Json; exit
 }
 $pidValue=[int]$gateState.gatePid; $waiting=0
 for($i=0;$i -lt 80;$i++){
  $query="WITH RECURSIVE waiting(pid) AS (SELECT pid FROM pg_stat_activity WHERE $pidValue=ANY(pg_blocking_pids(pid)) UNION SELECT a.pid FROM pg_stat_activity a JOIN waiting w ON w.pid=ANY(pg_blocking_pids(a.pid))) SELECT count(*) FROM waiting;"
  $waiting=[int](Invoke-DmReadSql $query).Trim()
  if($waiting -ge $ExpectedWaiters){break}; Start-Sleep -Milliseconds 50
 }
 if($waiting -lt $ExpectedWaiters){throw 'Expected database blocking chain not observed.'}
 @{held=$gateState.held;gatePid=$pidValue;waiters=$waiting;conversationId=$space} | ConvertTo-Json; exit
}
if($Action -in @('Keys','KeyStatus')){
 if($Action -eq 'KeyStatus'){
  Invoke-DmReadSql "SELECT json_build_object('operations',count(*),'keyIds',coalesce(json_agg(DISTINCT key_id),'[]'),'fingerprintVersions',coalesce(json_agg(DISTINCT fingerprint_version),'[]'),'validHashes',count(*) FILTER(WHERE octet_length(fingerprint)=32))::text FROM messaging.send_operations WHERE space_id='$space';"
  exit
 }
 if($KeyMode -eq 'Restore') {Invoke-DmCompose -Arguments @('up','-d','--no-deps','--force-recreate','chat-service')}
 else {
  $keyRoot=Join-Path $folder 'synthetic-hmac'; $complete=Join-Path $keyRoot 'complete'
  $missing1=Join-Path $keyRoot 'missing-k1'; $missing2=Join-Path $keyRoot 'missing-k2'
  foreach($dir in @($complete,$missing1,$missing2)){$null=New-Item -ItemType Directory -Path $dir -Force}
  $baseRing=Join-Path $paths.Runtime 'keyrings/hmac'
  foreach($key in @(@{name='p3-k1.key';bytes=[byte[]](0..31)},@{name='p3-k2.key';bytes=[byte[]](32..63)})){
   $keyFile=Join-Path $baseRing $key.name
   if(Test-Path $keyFile){if([Convert]::ToBase64String([IO.File]::ReadAllBytes($keyFile)) -cne [Convert]::ToBase64String($key.bytes)){throw 'Synthetic fixture key name collision; no existing key overwritten.'}}
   else{[IO.File]::WriteAllBytes($keyFile,$key.bytes)}
  }
  foreach($key in Get-ChildItem -LiteralPath $baseRing -Filter '*.key'){
   Copy-Item -LiteralPath $key.FullName -Destination (Join-Path $complete $key.Name) -Force
   if($key.Name -ne 'p3-k1.key'){Copy-Item -LiteralPath $key.FullName -Destination (Join-Path $missing1 $key.Name) -Force}
   if($key.Name -ne 'p3-k2.key'){Copy-Item -LiteralPath $key.FullName -Destination (Join-Path $missing2 $key.Name) -Force}
  }
  if((Test-Path (Join-Path $missing1 'p3-k1.key')) -or (Test-Path (Join-Path $missing2 'p3-k2.key'))){throw 'Missing-key fixture directory was altered. Use a fresh run; no key deleted.'}
  $selected=$complete; $keyId='p3-k2'; if($KeyMode -eq 'K1'){$keyId='p3-k1'}
  if($KeyMode -eq 'MissingK1'){$selected=$missing1}; if($KeyMode -eq 'MissingK2'){$selected=$missing2}
  $override=Join-Path $folder 'key-override.json'
  Write-DmJson $override @{services=@{'chat-service'=@{environment=@{Modules__Messaging__ActiveFingerprintKeyId=$keyId;Modules__Messaging__FingerprintKeyDirectory='/app/dm-proof-hmac'};volumes=@(($selected.Replace('\','/')+':/app/dm-proof-hmac:ro'))}}}
  Invoke-DmCompose -Arguments @('-f',$override,'up','-d','--no-deps','--force-recreate','chat-service')
 }
 Invoke-DmCompose -Arguments @('restart','web-client'); Ready
 Write-DmJson (Join-Path $folder 'key-mode.json') @{mode=$KeyMode;run=$Run;conversationId=$space}
 Write-Host "Acceptance key mode $KeyMode ready; no key/token printed. Original default is restored with -KeyMode Restore."
 exit
}
Assert-DmApiTarget
if($Action -eq 'Legacy'){
 if($MessageId -eq [guid]::Empty){throw 'Provide actual messageId from owned writer.'}
 $mid=$MessageId.ToString()
 $null=Sql @"
DO `$`$ BEGIN IF NOT EXISTS(SELECT 1 FROM messaging.messages m JOIN messaging.spaces s ON s.id=m.space_id WHERE m.id='$mid' AND m.space_id='$space' AND m.author_user_id='$actor' AND s.created_by_user_id='$actor') THEN RAISE EXCEPTION 'Owned message required'; END IF; END `$`$;
DELETE FROM messaging.send_operations WHERE message_id='$mid' AND space_id='$space' AND author_user_id='$actor';
UPDATE messaging.messages SET content='LEGACY-CURRENT-C04',edited_at=now(),version=version+1 WHERE id='$mid' AND space_id='$space';
"@
 $migration=Get-Content (Join-Path $paths.Repo 'database/postgres/migrations/20261009_dm_p2_text_message.sql') -Raw -Encoding UTF8
 $null=Sql $migration; $null=Sql $migration
 Write-Host 'Owned legacy operation backfilled without fingerprint; migration replayed twice, current body retained.'; exit
}
if($Action -eq 'Catchup'){
 if($Case -ne 'R101'){throw 'Catchup setup only applies to the R101 lane.'}
 $session=Connect-DmActor A $Run 'DM-P3-T02-catchup'
 try {
  $count=(Invoke-DmReadSql "SELECT count(*) FROM messaging.messages WHERE space_id='$space';").Trim()
  if($count -ne '0'){throw 'Use a fresh R101 pair; no reset performed.'}
  $first=Invoke-DmRequest GET "/direct-conversations/$space/messages?limit=50" -AccessToken $session.accessToken
  Assert-DmStatus $first 200 'Capture R101 baseline'
  $ids=@(); for($i=1;$i -le 101;$i++){
   $r=Invoke-DmRequest POST "/direct-conversations/$space/messages" @{clientMessageId=[guid]::NewGuid().ToString();content=('R101-{0:000}' -f $i)} -AccessToken $session.accessToken
   Assert-DmStatus $r 200 'R101 writer'; $ids+=@($r.Body.id)
  }
  $cursor=$first.Body.resumeCursor; $actual=@(); $sizes=@(); $pages=@()
  do {
   $r=Invoke-DmRequest GET ("/direct-conversations/$space/messages?limit=50&after="+[uri]::EscapeDataString($cursor)) -AccessToken $session.accessToken
   Assert-DmStatus $r 200 'R101 catchup page'
   $actual+=@($r.Body.items | ForEach-Object id); $sizes+=@($r.Body.items.Count); $pages+=@($r.Body)
   if(!$r.Body.hasMore){if(!$r.Body.resumeCursor){throw 'Final resume missing'};break}
   if($r.Body.resumeCursor){throw 'Resume advanced before final page'}; $cursor=$r.Body.nextCursor
  }while($true)
  if(($sizes -join ',') -ne '50,50,1' -or ($actual -join ',') -cne ($ids -join ',')){throw 'R101 union mismatch'}
  $proof=@{conversationId=$space;writerIds=$ids;sizes=$sizes;baselineCursor=$first.Body.resumeCursor;pages=$pages;result='PASS REST catchup, Hub reconnect deferred P4'}
  Write-DmJson (Join-Path $folder 'r101-proof.json') $proof
  @{conversationId=$space;sizes=$sizes;writerCount=$ids.Count;result=$proof.result} | ConvertTo-Json
 }finally{Logout $session}; exit
}
