[CmdletBinding()]
param(
 [Parameter(Mandatory)][ValidateSet('Setup','Fixtures','Concurrent','Current','Snapshot','FaultOn','FaultOff')][string]$Action,
 [string]$Run='p3user', [string]$PeerAlias='B', [guid]$ClientMessageId=[guid]::Empty,
 [string]$Content='LOST-RESPONSE-C02', [guid]$MessageId=[guid]::Empty,
 [ValidateSet('Edited','Deleted')][string]$State='Edited'
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$OutputEncoding=[System.Text.UTF8Encoding]::new($false)
Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force -DisableNameChecking
Assert-DmDatabase
$paths=Get-DmPaths
if ($Action -in @('Snapshot','FaultOn','FaultOff')) {
 & (Join-Path $PSScriptRoot 'send-text.ps1') -Action $Action -Run $Run -PeerAlias $PeerAlias
 exit
}
Assert-DmApiTarget
if ($Run -eq 'baseline') { throw 'Manual-retry setup/technical fixtures require a dedicated run; baseline is preserved.' }
$folder=Get-DmRunPath $Run
if ($Action -eq 'Setup') {
 if (Test-Path (Join-Path $folder 'manifest.json')) { throw 'Run already exists. Use Fixtures or choose a new run; no reset performed.' }
 Set-DmFixtures $Run
 exit
}
$manifest=Get-DmManifest $Run
function Account([string]$Alias) {
 $row=@($manifest.accounts | Where-Object alias -eq $Alias)
 if ($row.Count -ne 1) { throw 'Unknown fixture alias.' }
 return $row[0]
}
$a=Account A
$session=$null
if ($Action -in @('Fixtures','Concurrent')) { $session=Connect-DmActor A $Run 'DM-P3-manual-retry' }
try {
 if ($Action -eq 'Fixtures') {
  $lanes=@(@{case='C01';peer='B'},@{case='C02';peer='C'},@{case='C03';peer='S01'},@{case='C04';peer='S02'},@{case='DELAY';peer='S03'},@{case='CURRENT';peer='S04'},@{case='BE-C03';peer='S10'},@{case='BE-CONCURRENT';peer='S11'})
  $pairs=@()
  foreach ($lane in $lanes) {
   $peer=Account $lane.peer
   $r=Invoke-DmRequest POST /direct-conversations @{peerUserId=$peer.id} -AccessToken $session.accessToken
   Assert-DmStatus $r 200 'Open retry fixture'
   $pairs+=@([pscustomobject]@{case=$lane.case;peerAlias=$lane.peer;peerUsername=$peer.username;conversationId=$r.Body.id})
  }
  $data=@{run=$Run;actorId=$a.id;actorUsername=$a.username;pairs=$pairs;O1='7c8e7c59-b35a-4d12-b22f-965b96ff4e44';O2='7c8e7c59-b35a-4d12-b22f-965b96ff4e45'}
  Write-DmJson (Join-Path $folder 'retry-fixtures.json') $data
  $payloads=Join-Path $folder 'retry-payloads'
  $null=New-Item -ItemType Directory -Path $payloads -Force
  Write-DmJson (Join-Path $payloads 'original.json') @{clientMessageId=$data.O1;content='ORIGINAL-C03'}
  Write-DmJson (Join-Path $payloads 'altered.json') @{clientMessageId=$data.O1;content='ALTERED-C03'}
  Write-DmJson (Join-Path $payloads 'new-same-body.json') @{clientMessageId=$data.O2;content='ORIGINAL-C03'}
  $manifest.conversations=@($manifest.conversations | Where-Object { $_.id -notin @($pairs | ForEach-Object conversationId) })
  foreach ($pair in $pairs) { $manifest.conversations+=@([pscustomobject]@{id=$pair.conversationId;actorAlias='A';peerAlias=$pair.peerAlias;case=$pair.case}) }
  Write-DmJson (Join-Path $folder 'manifest.json') $manifest
  $data | ConvertTo-Json -Depth 6
  exit
 }
 $b=Account $PeerAlias
 $low,$high=@(([guid]$a.id).ToString(),([guid]$b.id).ToString() | Sort-Object)
 $space=(Invoke-DmReadSql "SELECT space_id::text FROM messaging.direct_conversations WHERE user_low_id='$low' AND user_high_id='$high';").Trim()
 if ($space -notmatch '^[a-f0-9-]{36}$') { throw 'Fixture pair missing. Run Fixtures first.' }
 if ($Action -eq 'Current') {
  if ($MessageId -eq [guid]::Empty) { throw 'Provide the actual message ID returned by this run writer.' }
  $author=([guid]$a.id).ToString(); $message=$MessageId.ToString()
  $body="content='CURRENT-EDITED-P3',edited_at=now(),version=version+1"
  if ($State -eq 'Deleted') { $body='content=NULL,deleted_at=now(),version=version+1' }
  $sql=@"
DO `$`$ BEGIN
 IF current_database()<>'scdc_dm_acceptance_test' OR NOT EXISTS(SELECT 1 FROM messaging.messages m JOIN messaging.spaces s ON s.id=m.space_id WHERE m.id='$message' AND m.space_id='$space' AND m.author_user_id='$author' AND s.created_by_user_id='$author') THEN RAISE EXCEPTION 'Owned test message required'; END IF;
 UPDATE messaging.messages SET $body WHERE id='$message' AND space_id='$space';
END `$`$;
"@
  $sql | & docker compose --project-name scdc-dm-acceptance --env-file $paths.Env -f (Join-Path $paths.Repo 'compose.dm-test.yaml') exec -T postgres psql -X -q -U scdc_dm_test -d scdc_dm_acceptance_test -v ON_ERROR_STOP=1
  if ($LASTEXITCODE -ne 0) { throw 'Owned current-state technical fixture failed.' }
  Write-Host "Current-state fixture $State applied only to owned message $message. No edit/delete production endpoint added."
  exit
 }
 if ($ClientMessageId -eq [guid]::Empty -or $ClientMessageId.ToString() -notmatch '^[a-f0-9]{8}-[a-f0-9]{4}-4[a-f0-9]{3}-[89ab][a-f0-9]{3}-[a-f0-9]{12}$') { throw 'Provide the UUIDv4 captured from the original operation.' }
 $client=[System.Net.Http.HttpClient]::new(); $client.Timeout=[TimeSpan]::FromSeconds(30)
 $httpRequests=@(); $httpResponses=@()
 try {
  $tasks=@()
  for($i=0;$i -lt 2;$i++) {
   $req=[System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post,(Get-DmUrls).Api+"/direct-conversations/$space/messages")
   $req.Content=[System.Net.Http.StringContent]::new((@{clientMessageId=$ClientMessageId.ToString();content=$Content} | ConvertTo-Json),[System.Text.Encoding]::UTF8,'application/json')
   $req.Headers.Authorization=[System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer',$session.accessToken)
   $httpRequests+=@($req); $tasks+=@($client.SendAsync($req))
  }
  $ids=@(); $statuses=@()
  foreach($task in $tasks) {
   $r=$task.GetAwaiter().GetResult(); $httpResponses+=@($r); $statuses+=@([int]$r.StatusCode)
   if ([int]$r.StatusCode -ne 200) { throw 'Concurrent explicit retry did not return200; response body suppressed.' }
   $body=$r.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
   if ($body.clientMessageId -cne $ClientMessageId.ToString()) { throw 'Concurrent response operation mismatch.' }
   $ids+=@($body.id)
  }
  if (@($ids | Select-Object -Unique).Count -ne 1) { throw 'Concurrent retries returned different IDs.' }
  $author=([guid]$a.id).ToString(); $uuid=$ClientMessageId.ToString()
  $counts=Invoke-DmReadSql "SELECT json_build_object('messages',(SELECT count(*) FROM messaging.messages WHERE space_id='$space' AND author_user_id='$author' AND client_message_id='$uuid'),'operations',(SELECT count(*) FROM messaging.send_operations WHERE space_id='$space' AND author_user_id='$author' AND client_message_id='$uuid'),'createOutbox',(SELECT count(*) FROM integration.outbox_events WHERE space_id='$space' AND aggregate_id='$( $ids[0] )' AND aggregate_version=1))::text;" | ConvertFrom-Json
  if ($counts.messages -ne 1 -or $counts.operations -ne 1 -or $counts.createOutbox -ne 1) { throw 'Concurrent retry counts mismatch.' }
  $proof=@{conversationId=$space;clientMessageId=$uuid;statuses=$statuses;messageIds=$ids;counts=$counts;result='PASS'}
  Write-DmJson (Join-Path $folder "retry-concurrent-$PeerAlias.json") $proof
  $proof | ConvertTo-Json -Depth 7
 } finally { foreach($r in $httpResponses) { $r.Dispose() }; foreach($req in $httpRequests) { $req.Dispose() }; $client.Dispose() }
} finally { if ($null -ne $session) { $null=Invoke-DmRequest POST /auth/logout @{refreshToken=$session.refreshToken} } }
