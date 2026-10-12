[CmdletBinding()]
param(
 [Parameter(Mandatory)][ValidateSet('Setup','Fixtures','Snapshot','Seed101','Send','Patch','Delete','State')][string]$Action,
 [string]$Run='mvpuser',
 [ValidateSet('C01','C02','C03','C04','C05','C06','C07','C08','C09','C10','C02P1','C02P2','C02LIVE','MOBILE','EDITOLD','DELETEOLD')][string]$Case='C01',
 [string]$Alias='A', [guid]$MessageId=[guid]::Empty, [guid]$ClientMessageId=[guid]::Empty,
 [string]$Content='MVP-M01', [string]$ExpectedVersion='1', [int]$ExpectedStatus=200,
 [ValidateSet('Disabled','Unlock','Unverified','Verify','Stamp','Expire')][string]$State='Disabled',
 [guid]$SessionId=[guid]::Empty
)
Set-StrictMode -Version Latest; $ErrorActionPreference='Stop'; $OutputEncoding=[Text.UTF8Encoding]::new($false)
Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force -DisableNameChecking
Assert-DmDatabase; $paths=Get-DmPaths; $folder=Get-DmRunPath $Run
if($Run -eq 'baseline'){throw 'Use a dedicated MVP run'}
function Sql([string]$Query){
 $result=$Query | & docker compose --project-name scdc-dm-acceptance --env-file $paths.Env -f (Join-Path $paths.Repo 'compose.dm-test.yaml') exec -T postgres psql -X -qAt -U scdc_dm_test -d scdc_dm_acceptance_test -v ON_ERROR_STOP=1
 if($LASTEXITCODE -ne 0){throw 'Owned MVP SQL failed'}; return ($result -join "`n")
}
if($Action -eq 'Setup'){
 Assert-DmApiTarget
 if(Test-Path (Join-Path $folder 'manifest.json')){throw 'Run exists; no reset performed'}
 Set-DmFixtures $Run; exit
}
$manifest=Get-DmManifest $Run; $accounts=@($manifest.accounts); $actor=@($accounts|Where-Object alias -eq $Alias)
if($actor.Count -ne 1){throw 'Unknown alias'}; $actorId=([guid]$actor[0].id).ToString()
if($actor[0].username -notmatch '^[a-z0-9_.]{3,32}$' -or !$actor[0].username.EndsWith('_'+$Run.Replace('-','_'))){throw 'Actor username does not belong to this run'}
$file=Join-Path $folder 'mvp-fixtures.json'
if($Action -eq 'State'){
 $target=Sql "SELECT count(*) FROM identity.users WHERE id='$actorId' AND username='$($actor[0].username)';"
 if($target.Trim() -ne '1'){throw 'Run actor missing'}
 $sidFilter=''; if($SessionId -ne [guid]::Empty){$sidFilter=" AND id='$($SessionId.ToString())'"}
 $change=switch($State){
  Disabled {"UPDATE identity.users SET status=3 WHERE id='$actorId'; UPDATE identity.user_security_states SET security_stamp=gen_random_uuid() WHERE user_id='$actorId'; UPDATE identity.auth_sessions SET revoked_at=clock_timestamp(),revoke_reason='owned_mvp_disabled' WHERE user_id='$actorId' AND revoked_at IS NULL;"}
  Unlock {"UPDATE identity.users SET status=1 WHERE id='$actorId';"}
  Unverified {"UPDATE identity.user_emails SET verified_at=NULL WHERE user_id='$actorId' AND is_primary;"}
  Verify {"UPDATE identity.user_emails SET verified_at=clock_timestamp() WHERE user_id='$actorId' AND is_primary;"}
  Stamp {"UPDATE identity.user_security_states SET security_stamp=gen_random_uuid() WHERE user_id='$actorId';"}
  Expire {"UPDATE identity.auth_sessions SET expires_at=clock_timestamp() WHERE user_id='$actorId' AND revoked_at IS NULL$sidFilter;"}
 }
 $proof=Sql "BEGIN; SELECT id FROM identity.users WHERE id='$actorId' FOR UPDATE; $change COMMIT; SELECT json_build_object('userId','$actorId','state','$State','commitConfirmedAt',clock_timestamp(),'status',(SELECT status FROM identity.users WHERE id='$actorId'))::text;"
 $json=@($proof -split "`n"|Where-Object{$_ -match '^\{'})[-1] | ConvertFrom-Json
 Write-DmJson (Join-Path $folder ('state-'+$Alias+'-'+$State+'.json')) $json; $json|ConvertTo-Json; exit
}
Assert-DmApiTarget
if($Action -eq 'Fixtures'){
 if(Test-Path $file){throw 'MVP fixture already exists; use a new run'}
 $session=Connect-DmActor A $Run 'MVP-fixtures'
 try{
  $cases=@('C01','C02','C03','C04','C05','C06','C07','C08','C09','C10','C02P1','C02P2','C02LIVE','MOBILE','EDITOLD','DELETEOLD')
  $aliases=@('B','C','S01','S02','S03','S04','S05','S06','S07','K','S08','S09','S10','S11','S12','S13'); $pairs=@()
  for($i=0;$i -lt $cases.Count;$i++){
   $peer=@($accounts|Where-Object alias -eq $aliases[$i])[0]
   $opened=Invoke-DmRequest POST /direct-conversations @{peerUserId=$peer.id} -AccessToken $session.accessToken; Assert-DmStatus $opened 200 'MVP fixture open'
   $pairs+=@(@{case=$cases[$i];peerAlias=$aliases[$i];peerUsername=$peer.username;conversationId=$opened.Body.id;X=[guid]::NewGuid().ToString();Y=[guid]::NewGuid().ToString()})
  }
  $fixture=@{run=$Run;actorUsername=@($accounts|Where-Object alias -eq A)[0].username;pairs=$pairs}; Write-DmJson $file $fixture
  Write-Host "MVP $Run created: 16 empty owned conversation lanes; no messages seeded."
 }finally{$null=Invoke-DmRequest POST /auth/logout @{refreshToken=$session.refreshToken}}; exit
}
$fixture=Get-Content $file -Raw -Encoding UTF8|ConvertFrom-Json; $pair=@($fixture.pairs|Where-Object case -eq $Case)[0];$space=([guid]$pair.conversationId).ToString()
if($Action -eq 'Snapshot'){
 Invoke-DmReadSql @"
SELECT json_build_object('conversationId',s.id,'counter',s.last_message_sequence::text,
 'messages',(SELECT count(*) FROM messaging.messages WHERE space_id=s.id),'operations',(SELECT count(*) FROM messaging.send_operations WHERE space_id=s.id),
 'outbox',(SELECT count(*) FROM integration.outbox_events WHERE space_id=s.id),'createOutbox',(SELECT count(*) FROM integration.outbox_events WHERE space_id=s.id AND aggregate_version=1),
 'pending',(SELECT count(*) FROM integration.outbox_events WHERE space_id=s.id AND published_at IS NULL),
 'bodyCopies',(SELECT count(*) FROM integration.outbox_events WHERE space_id=s.id AND (payload ? 'content' OR payload ? 'body')),
 'editHistory',(SELECT count(*) FROM messaging.message_edits e JOIN messaging.messages m ON m.id=e.message_id WHERE m.space_id=s.id),
 'rows',(SELECT coalesce(json_agg(json_build_object('id',m.id,'clientMessageId',m.client_message_id,'sequence',m.conversation_sequence::text,'version',m.version::text,'deleted',m.deleted_at IS NOT NULL) ORDER BY m.conversation_sequence),'[]') FROM messaging.messages m WHERE m.space_id=s.id),
 'events',(SELECT coalesce(json_agg(json_build_object('id',o.id,'messageId',o.aggregate_id,'version',o.aggregate_version,'published',o.published_at IS NOT NULL,'attempts',o.attempt_count,'leaseToken',o.lease_token,'leaseExpiresAt',o.lease_expires_at,'lastError',o.last_error) ORDER BY o.occurred_at,o.id),'[]') FROM integration.outbox_events o WHERE o.space_id=s.id))::text FROM messaging.spaces s WHERE s.id='$space';
"@;exit
}
$auth=Connect-DmActor $Alias $Run ('MVP-'+$Action)
try{
 if($Action -eq 'Seed101'){
  $count=(Invoke-DmReadSql "SELECT count(*) FROM messaging.messages WHERE space_id='$space';").Trim()
  if($count -notin @('0','1')){throw 'Seed101 requires a fresh lane or exactly one old message; no reset performed'}
  $receipts=@()
  for($i=1;$i -le 101;$i++){
   $op=[guid]::NewGuid().ToString(); $reply=Invoke-DmRequest POST "/direct-conversations/$space/messages" @{clientMessageId=$op;content=('RC-'+$i.ToString('D3'))} -AccessToken $auth.accessToken;Assert-DmStatus $reply 200 'Seed101 actual writer'
   $receipts+=@(@{id=$reply.Body.id;clientMessageId=$op;sequence=$reply.Body.sequence;version=$reply.Body.version})
  }
  Write-DmJson (Join-Path $folder ('seed-'+$Case+'.json')) @{case=$Case;conversationId=$space;before=[int]$count;receipts=$receipts;count=101}
  Write-Host '101 actual API writes committed; only ID/sequence/UUID metadata saved.';exit
 }
 $url="/direct-conversations/$space/messages"
 if($Action -eq 'Send'){
  if($ClientMessageId -eq [guid]::Empty){$ClientMessageId=[guid]::NewGuid()}
  $reply=Invoke-DmRequest POST $url @{clientMessageId=$ClientMessageId.ToString();content=$Content} -AccessToken $auth.accessToken
 }else{
  if($MessageId -eq [guid]::Empty){throw 'Provide actual MessageId'};$url+='/'+$MessageId.ToString()
  if($Action -eq 'Patch'){$reply=Invoke-DmRequest PATCH $url @{content=$Content;expectedVersion=$ExpectedVersion} -AccessToken $auth.accessToken}
  else{$reply=Invoke-DmRequest DELETE ($url+'?expectedVersion='+[uri]::EscapeDataString($ExpectedVersion)) -AccessToken $auth.accessToken}
 }
 Assert-DmStatus $reply $ExpectedStatus 'MVP mutation'
 $code=$null;if($reply.Body.PSObject.Properties['errorCode']){$code=$reply.Body.errorCode}
 if($reply.Status -eq 200){[pscustomobject]@{status=$reply.Status;id=$reply.Body.id;clientMessageId=$reply.Body.clientMessageId;sequence=$reply.Body.sequence;version=$reply.Body.version;deleted=($null -ne $reply.Body.deletedAt)}}
 else{[pscustomobject]@{status=$reply.Status;errorCode=$code}}
}finally{$null=Invoke-DmRequest POST /auth/logout @{refreshToken=$auth.refreshToken}}
