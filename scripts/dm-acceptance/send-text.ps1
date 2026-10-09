[CmdletBinding()]
param(
 [Parameter(Mandatory)][ValidateSet('Upgrade','Setup','Payloads','Open','Send','Snapshot','FaultOn','FaultOff')][string]$Action,
 [string]$Run='baseline', [string]$ActorAlias='A', [string]$PeerAlias='B', [string]$SenderAlias='',
 [string]$Content='M01: Chao Bao, minh la An.', [guid]$ClientMessageId=[guid]::Empty,
 [string]$RequestFile, [string]$Lane='manual', [int]$ExpectedStatus=200, [switch]$Anonymous
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force -DisableNameChecking
Assert-DmDatabase
$paths=Get-DmPaths
$OutputEncoding=[System.Text.UTF8Encoding]::new($false)
function Invoke-TextSql([string]$Sql) {
 $Sql | & docker compose --project-name scdc-dm-acceptance --env-file $paths.Env -f (Join-Path $paths.Repo 'compose.dm-test.yaml') exec -T postgres psql -X -q -U scdc_dm_test -d scdc_dm_acceptance_test -v ON_ERROR_STOP=1
 if ($LASTEXITCODE -ne 0) { throw 'Scoped acceptance SQL failed.' }
}
if ($Action -eq 'FaultOff') {
 Invoke-TextSql 'DROP TRIGGER IF EXISTS dm_acceptance_text_fault ON integration.outbox_events; DROP FUNCTION IF EXISTS integration.dm_acceptance_text_fault();'
 Write-Host 'Outbox insertion fault removed.'
 exit
}
if ($Action -eq 'Upgrade') {
 Invoke-TextSql (Get-Content (Join-Path $paths.Repo 'database/postgres/migrations/20261009_dm_p2_text_message.sql') -Raw -Encoding UTF8)
 Invoke-DmCompose -Arguments @('build','chat-service','web-client')
 & (Join-Path $PSScriptRoot '../dm-acceptance.ps1') -Action Start -NoBuild
 exit
}
Assert-DmApiTarget
if ($Action -eq 'Setup') { Set-DmFixtures $Run; exit }
if ($Action -eq 'Payloads') {
 if ($Lane -notmatch '^[a-z0-9_-]{1,24}$') { throw 'Lane must use 1-24 lowercase ASCII letters/digits/underscore/hyphen.' }
 $folder=Join-Path (Get-DmRunPath $Run) "payloads-$Lane"
 if (Test-Path (Join-Path $folder 'cases.json')) { throw 'Payload lane already exists. Keep IDs for replay; choose a new Lane for independent sends.' }
 & node (Join-Path $PSScriptRoot 'text-payloads.mjs') $paths.Repo $folder
 if ($LASTEXITCODE -ne 0) { throw 'Payload generation failed.' }
 Write-Host $folder
 exit
}
$manifest=Get-DmManifest $Run
$actor=@($manifest.accounts | Where-Object alias -eq $ActorAlias)
$peer=@($manifest.accounts | Where-Object alias -eq $PeerAlias)
if ($actor.Count -ne 1 -or $peer.Count -ne 1) { throw 'Unknown fixture alias.' }
$actorId=([guid]$actor[0].id).ToString()
$peerId=([guid]$peer[0].id).ToString()
$low,$high=@($actorId,$peerId | Sort-Object)
$pair="user_low_id='$low' AND user_high_id='$high'"
if ($Action -eq 'Open') {
 $session=Connect-DmActor $ActorAlias $Run 'DM-P2-open'
 try {
  $response=Invoke-DmRequest POST /direct-conversations @{peerUserId=$peerId} -AccessToken $session.accessToken
  Assert-DmStatus $response 200 'Open DM'
  $response.Body | ConvertTo-Json -Depth 8
 } finally { $null=Invoke-DmRequest POST /auth/logout @{refreshToken=$session.refreshToken} }
 exit
}
$space=(Invoke-DmReadSql "SELECT space_id::text FROM messaging.direct_conversations WHERE $pair;").Trim()
if ($space -notmatch '^[a-f0-9-]{36}$') { throw 'Open the fixture pair first with -Action Open.' }
function Get-TextSnapshot {
 $query=@"
SELECT json_build_object('conversationId',s.id,'messageCount',(SELECT count(*) FROM messaging.messages WHERE space_id=s.id),
 'operationCount',(SELECT count(*) FROM messaging.send_operations WHERE space_id=s.id),
 'outboxCount',(SELECT count(*) FROM integration.outbox_events WHERE space_id=s.id),
 'lastSequence',s.last_message_sequence::text,'lastMessageId',s.last_message_id,'lastActivityAt',s.last_activity_at,
 'messages',(SELECT coalesce(json_agg(json_build_object('id',m.id,'clientMessageId',m.client_message_id,'authorId',m.author_user_id,'sequence',m.conversation_sequence::text,'version',m.version::text,'utf8Bytes',octet_length(m.content)) ORDER BY m.conversation_sequence),'[]') FROM messaging.messages m WHERE m.space_id=s.id),
 'outboxContainsBody',(SELECT count(*) FROM integration.outbox_events WHERE space_id=s.id AND (payload ? 'content' OR payload ? 'body')),
 'outboxPayloads',(SELECT coalesce(json_agg(payload),'[]') FROM integration.outbox_events WHERE space_id=s.id))::text
FROM messaging.spaces s WHERE s.id='$space';
"@
 return (Invoke-DmReadSql $query | ConvertFrom-Json)
}
switch ($Action) {
 Snapshot { Get-TextSnapshot | ConvertTo-Json -Depth 10 }
 FaultOn {
  Invoke-TextSql @"
CREATE OR REPLACE FUNCTION integration.dm_acceptance_text_fault() RETURNS trigger LANGUAGE plpgsql AS `$`$
BEGIN
 IF current_database()<>'scdc_dm_acceptance_test' THEN RAISE EXCEPTION 'Test database required'; END IF;
 IF NEW.space_id='$space' THEN RAISE EXCEPTION USING ERRCODE='58000', MESSAGE='Scoped acceptance outbox insertion fault'; END IF;
 RETURN NEW;
END; `$`$;
DROP TRIGGER IF EXISTS dm_acceptance_text_fault ON integration.outbox_events;
CREATE TRIGGER dm_acceptance_text_fault BEFORE INSERT ON integration.outbox_events FOR EACH ROW EXECUTE FUNCTION integration.dm_acceptance_text_fault();
"@
  Write-Host "Fault enabled before commit for run $Run pair $ActorAlias/$PeerAlias only. Always run FaultOff."
 }
 Send {
  $session=$null
  if (-not $Anonymous) { $sender=$ActorAlias; if ($SenderAlias) { $sender=$SenderAlias }; $session=Connect-DmActor $sender $Run 'DM-P2-send' }
  $before=Get-TextSnapshot
  try {
   $raw=$null
   if ($RequestFile) { $raw=[System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $RequestFile).Path,[System.Text.Encoding]::UTF8) }
   else {
    if ($ClientMessageId -eq [guid]::Empty) { $ClientMessageId=[guid]::NewGuid() }
    $raw=@{clientMessageId=$ClientMessageId.ToString();content=$Content} | ConvertTo-Json
   }
   $client=[System.Net.Http.HttpClient]::new()
   $client.Timeout=[TimeSpan]::FromSeconds(30)
   $request=[System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, (Get-DmUrls).Api+"/direct-conversations/$space/messages")
   $request.Content=[System.Net.Http.StringContent]::new($raw,[System.Text.Encoding]::UTF8,'application/json')
   if ($null -ne $session) { $request.Headers.Authorization=[System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer',$session.accessToken) }
   try {
    $http=$client.SendAsync($request).GetAwaiter().GetResult()
    try { $response=[pscustomobject]@{Status=[int]$http.StatusCode;Body=($http.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json)} }
    finally { $http.Dispose() }
   } finally { $request.Dispose(); $client.Dispose() }
   Assert-DmStatus $response $ExpectedStatus 'Send text'
   $after=Get-TextSnapshot
   if ($ExpectedStatus -ne 200 -and ($before | ConvertTo-Json -Compress -Depth 10) -cne ($after | ConvertTo-Json -Compress -Depth 10)) { throw 'Rejected send changed message/operation/outbox/counter/activity.' }
   $response | ConvertTo-Json -Depth 8
   $after | ConvertTo-Json -Depth 10
  } finally { if ($null -ne $session) { $null=Invoke-DmRequest POST /auth/logout @{refreshToken=$session.refreshToken} } }
 }
}
