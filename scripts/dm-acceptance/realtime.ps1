[CmdletBinding()]
param(
 [Parameter(Mandatory)][ValidateSet('Upgrade','Setup','Fixtures','Snapshot','Pause','Resume','HoldClaim','ClaimStatus','ReleaseClaim','Requeue','Current','FaultOn','FaultOff','HubProof')][string]$Action,
 [string]$Run='p4user', [ValidateSet('C01','C02','C03','C04','CRASH','ROLLBACK','SESSION','CURRENT')][string]$Case='C01',
 [guid]$MessageId=[guid]::Empty, [guid]$EventId=[guid]::Empty, [ValidateSet('Edited','Deleted')][string]$State='Edited'
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$OutputEncoding=[Text.UTF8Encoding]::new($false)
Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force -DisableNameChecking
Assert-DmDatabase
$paths=Get-DmPaths; $folder=Get-DmRunPath $Run; $control=Join-Path $paths.Runtime 'dispatcher'
if($Run -eq 'baseline'){throw 'Use a dedicated realtime run.'}
function Sql([string]$Query){
 $Query | & docker compose --project-name scdc-dm-acceptance --env-file $paths.Env -f (Join-Path $paths.Repo 'compose.dm-test.yaml') exec -T postgres psql -X -qAt -U scdc_dm_test -d scdc_dm_acceptance_test -v ON_ERROR_STOP=1
 if($LASTEXITCODE -ne 0){throw 'Acceptance SQL failed.'}
}
if($Action -eq 'Upgrade'){
 $migration=Get-Content (Join-Path $paths.Repo 'database/postgres/migrations/20261010_dm_p4_outbox_lease.sql') -Raw -Encoding UTF8
 $null=Sql $migration; $null=Sql $migration
 $null=New-Item -ItemType Directory -Path $control -Force
 Invoke-DmCompose -Arguments @('build','chat-service','web-client')
 & (Join-Path $PSScriptRoot '../dm-acceptance.ps1') -Action Start -NoBuild
 if($LASTEXITCODE -ne 0){throw 'Start failed'}; exit
}
if($Action -in @('Pause','Resume')){
 Assert-DmApiTarget; $null=New-Item -ItemType Directory -Path $control -Force
 $pause=Join-Path $control 'pause'
 if($Action -eq 'Pause'){
  $nonce=[guid]::NewGuid().ToString(); [IO.File]::WriteAllText($pause,$nonce,[Text.UTF8Encoding]::new($false))
  $ack=Join-Path $control 'pause-ack.json'; $ready=$false
  for($i=0;$i -lt 100;$i++){
   if(Test-Path -LiteralPath $ack){try{$data=Get-Content $ack -Raw -Encoding UTF8 | ConvertFrom-Json; if($data.nonce -eq $nonce){$ready=$true;break}}catch{}}
   Start-Sleep -Milliseconds 100
  }; if(!$ready){throw 'Dispatcher pause was not acknowledged. Resume cleanup is still required.'}
 }
 elseif(Test-Path -LiteralPath $pause){Remove-Item -LiteralPath $pause}
 Write-Host "Dispatcher $Action requested. API/DB/Hub transport remain running; not a delivery receipt."; exit
}
if($Action -notin @('Snapshot','ClaimStatus','ReleaseClaim')){Assert-DmApiTarget}
if($Action -eq 'Setup'){
 if(Test-Path (Join-Path $folder 'manifest.json')){throw 'Run exists; no reset performed.'}
 Set-DmFixtures $Run; exit
}
$manifest=Get-DmManifest $Run; $actor=@($manifest.accounts | Where-Object alias -eq A)[0]
$fixtureFile=Join-Path $folder 'realtime-fixtures.json'
if($Action -eq 'Fixtures'){
 if(Test-Path $fixtureFile){throw 'Fixture already exists. Use a new run for independent mutations.'}
 $session=Connect-DmActor A $Run 'DM-P4-T01-fixtures'
 try {
  $pairs=@(); $cases=@('C01','C02','C03','C04','CRASH','ROLLBACK','SESSION','CURRENT'); $aliases=@('B','C','S01','S02','S03','S04','S05','S06')
  for($i=0;$i -lt $cases.Count;$i++){
   $peer=@($manifest.accounts | Where-Object alias -eq $aliases[$i])[0]
   $opened=Invoke-DmRequest POST /direct-conversations @{peerUserId=$peer.id} -AccessToken $session.accessToken
   Assert-DmStatus $opened 200 'Create fixture pair'
   $pairs+=@(@{case=$cases[$i];peerAlias=$aliases[$i];peerUsername=$peer.username;conversationId=$opened.Body.id;X=[guid]::NewGuid().ToString();Y=[guid]::NewGuid().ToString()})
  }
  $fixtures=@{run=$Run;actorId=$actor.id;actorUsername=$actor.username;pairs=$pairs}; Write-DmJson $fixtureFile $fixtures
  $fixtures | ConvertTo-Json -Depth 8
 }finally{$null=Invoke-DmRequest POST /auth/logout @{refreshToken=$session.refreshToken}}; exit
}
$fixtures=Get-Content $fixtureFile -Raw -Encoding UTF8 | ConvertFrom-Json
$lane=@($fixtures.pairs | Where-Object case -eq $Case)[0]; $space=([guid]$lane.conversationId).ToString(); $actorId=([guid]$actor.id).ToString()
if($Action -in @('FaultOn','FaultOff')){
 & (Join-Path $PSScriptRoot 'send-text.ps1') -Action $Action -Run $Run -PeerAlias $lane.peerAlias
 if($LASTEXITCODE -ne 0){throw 'Scoped fault action failed'}; exit
}
if($Action -eq 'HubProof'){
 & node (Join-Path $PSScriptRoot 'realtime-hub.mjs') $Run $Case
 if($LASTEXITCODE -ne 0){throw 'Hub proof failed'}; exit
}
if($Action -eq 'Snapshot'){
 Invoke-DmReadSql @"
SELECT json_build_object('conversationId',s.id,'counter',s.last_message_sequence::text,
 'messages',(SELECT count(*) FROM messaging.messages WHERE space_id=s.id),
 'operations',(SELECT count(*) FROM messaging.send_operations WHERE space_id=s.id),
 'outbox',(SELECT count(*) FROM integration.outbox_events WHERE space_id=s.id),
 'pending',(SELECT count(*) FROM integration.outbox_events WHERE space_id=s.id AND published_at IS NULL),
 'bodyCopies',(SELECT count(*) FROM integration.outbox_events WHERE space_id=s.id AND (payload ? 'content' OR payload ? 'body')),
 'rows',(SELECT coalesce(json_agg(json_build_object('id',m.id,'clientMessageId',m.client_message_id,'sequence',m.conversation_sequence::text,'version',m.version::text,'deleted',m.deleted_at IS NOT NULL) ORDER BY m.conversation_sequence),'[]') FROM messaging.messages m WHERE m.space_id=s.id),
 'events',(SELECT coalesce(json_agg(json_build_object('id',o.id,'messageId',o.aggregate_id,'published',o.published_at IS NOT NULL,'attempts',o.attempt_count,'leaseToken',o.lease_token,'leaseExpiresAt',o.lease_expires_at,'lastError',o.last_error)),'[]') FROM integration.outbox_events o WHERE o.space_id=s.id))::text FROM messaging.spaces s WHERE s.id='$space';
"@; exit
}
if($Action -eq 'Current'){
 if($MessageId -eq [guid]::Empty){throw 'Provide actual owned messageId'}; $mid=$MessageId.ToString()
 $content="'CURRENT-P4-C03'"; $deleted='NULL'; if($State -eq 'Deleted'){$content='NULL';$deleted='clock_timestamp()'}
 $changed=Sql "UPDATE messaging.messages m SET content=$content,edited_at=clock_timestamp(),deleted_at=$deleted,version=m.version+1 FROM messaging.spaces s WHERE m.id='$mid' AND m.space_id='$space' AND m.author_user_id='$actorId' AND s.id=m.space_id AND s.created_by_user_id='$actorId' RETURNING m.id;"
 if($changed.Trim() -ne $mid){throw 'Owned message not found'}
 Write-Host 'Owned technical current-state fixture changed. Not an edit/delete production endpoint; operation/outbox body unchanged.'; exit
}
if($EventId -eq [guid]::Empty){throw 'Provide actual eventId from Snapshot.'}; $eid=$EventId.ToString()
$owned=(Invoke-DmReadSql "SELECT count(*) FROM integration.outbox_events o JOIN messaging.messages m ON m.id=o.aggregate_id AND m.space_id=o.space_id JOIN messaging.spaces s ON s.id=o.space_id WHERE o.id='$eid' AND o.space_id='$space' AND m.author_user_id='$actorId' AND s.created_by_user_id='$actorId';").Trim()
if($owned -ne '1'){throw 'Event does not belong to this run/actor/conversation'}
$null=New-Item -ItemType Directory -Path $control -Force
$hold=Join-Path $control ('hold-'+$eid); $claimed=Join-Path $control ('claimed-'+$eid+'.json')
switch($Action){
 HoldClaim {[IO.File]::WriteAllText($hold,'Local owned event barrier',[Text.UTF8Encoding]::new($false)); Write-Host 'Claim barrier armed before publish; Resume dispatcher to reach it.'}
 ClaimStatus {if(!(Test-Path -LiteralPath $claimed)){throw 'Claim marker not ready'}; Get-Content $claimed -Raw -Encoding UTF8}
 ReleaseClaim {if(Test-Path -LiteralPath $hold){Remove-Item -LiteralPath $hold}; Write-Host 'Owned claim barrier released.'}
 Requeue {$null=Sql "UPDATE integration.outbox_events SET published_at=NULL,available_at=clock_timestamp(),lease_token=NULL,lease_expires_at=NULL,last_error=NULL WHERE id='$eid' AND space_id='$space';";Write-Host 'Owned event requeued with same ID. No writer POST/message/operation created.'}
}
