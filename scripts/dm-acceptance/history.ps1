[CmdletBinding()]
param(
 [Parameter(Mandatory)][ValidateSet('Setup','Fixtures','Page','Compare','CursorChecks','RestartProof')][string]$Action,
 [string]$Run='p2hist', [string]$ActorAlias='A', [string]$PeerAlias='S12', [string]$ReaderAlias='',
 [ValidateRange(1,100)][int]$Limit=50, [string]$Before, [string]$After, [string]$Through,
 [int]$ExpectedStatus=200, [switch]$Anonymous
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$OutputEncoding=[System.Text.UTF8Encoding]::new($false)
Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force -DisableNameChecking
Assert-DmDatabase
Assert-DmApiTarget
$paths=Get-DmPaths
if ($Action -eq 'Setup') { Set-DmFixtures $Run; exit }
$manifest=Get-DmManifest $Run
$folder=Get-DmRunPath $Run
function Account([string]$Alias) {
 $found=@($manifest.accounts | Where-Object alias -eq $Alias)
 if ($found.Count -ne 1) { throw 'Unknown alias.' }
 return $found[0]
}
function Resolve-Pair([string]$Peer) {
 $a=([guid](Account $ActorAlias).id).ToString(); $b=([guid](Account $Peer).id).ToString()
 $low,$high=@($a,$b | Sort-Object)
 $id=(Invoke-DmReadSql "SELECT space_id::text FROM messaging.direct_conversations WHERE user_low_id='$low' AND user_high_id='$high';").Trim()
 if ($id -notmatch '^[a-f0-9-]{36}$') { throw 'Fixture pair missing; run Fixtures or send-text Open.' }
 return $id
}
function Read-Page($Session,[string]$Space,[string]$Direction='', [string]$Cursor='', [int]$Size=50) {
 $query="?limit=$Size"
 if ($Direction) { $query+="&$Direction="+[uri]::EscapeDataString($Cursor) }
 return (Invoke-DmRequest GET "/direct-conversations/$Space/messages$query" -AccessToken $Session.accessToken)
}
function Read-Json([string]$Space) {
 return (Invoke-DmReadSql "SELECT coalesce(json_agg(json_build_object('id',id,'sequence',conversation_sequence::text,'version',version::text) ORDER BY conversation_sequence),'[]')::text FROM messaging.messages WHERE space_id='$Space';")
}
function Read-Ids([string]$Space) { return @((Read-Json $Space) | ConvertFrom-Json) }
if ($Action -eq 'Fixtures') {
 if ($Run -eq 'baseline') { throw 'Use a dedicated history run; baseline is preserved.' }
 $session=Connect-DmActor $ActorAlias $Run 'DM-history-fixture'
 try {
  foreach ($peer in @('S12','S13')) {
   $opened=Invoke-DmRequest POST /direct-conversations @{peerUserId=(Account $peer).id} -AccessToken $session.accessToken
   Assert-DmStatus $opened 200 'Open history fixture'
  }
  $space=Resolve-Pair 'S12'
  $file=Join-Path $folder 'history-h121.json'
  $record=[ordered]@{conversationId=$space;actorAlias=$ActorAlias;peerAlias='S12';messages=@()}
  if (Test-Path -LiteralPath $file) { $record=Get-Content -LiteralPath $file -Raw -Encoding UTF8 | ConvertFrom-Json }
  $rows=Read-Ids $space
  if ($rows.Count -ne @($record.messages).Count) { throw 'H121 data differs from manifest. Preserve it and choose a new run.' }
  for ($i=$rows.Count+1;$i -le 121;$i++) {
   $uuid=[guid]::NewGuid().ToString(); $label='HIST-'+$i.ToString('000')
   $response=Invoke-DmRequest POST "/direct-conversations/$space/messages" @{clientMessageId=$uuid;content=$label} -AccessToken $session.accessToken
   Assert-DmStatus $response 200 'Seed through actual writer'
   $record.messages+=@([pscustomobject]@{id=$response.Body.id;clientMessageId=$uuid;sequence=$response.Body.sequence;version=$response.Body.version;label=$label})
   Write-DmJson $file $record
  }
  $big=Resolve-Pair 'S13'; $bigFile=Join-Path $folder 'history-bigint.json'
  if (-not (Test-Path -LiteralPath $bigFile)) {
   # Technical setup only: owned, empty pair in the verified isolated test database.
   $a=([guid](Account $ActorAlias).id).ToString()
   $sql=@"
DO `$`$ BEGIN
 IF current_database()<>'scdc_dm_acceptance_test' OR NOT EXISTS(SELECT 1 FROM messaging.spaces WHERE id='$big' AND created_by_user_id='$a') OR EXISTS(SELECT 1 FROM messaging.messages WHERE space_id='$big') THEN RAISE EXCEPTION 'Owned empty test pair required'; END IF;
 UPDATE messaging.spaces SET last_message_sequence=9007199254740991 WHERE id='$big';
END `$`$;
"@
   $sql | & docker compose --project-name scdc-dm-acceptance --env-file $paths.Env -f (Join-Path $paths.Repo 'compose.dm-test.yaml') exec -T postgres psql -X -q -U scdc_dm_test -d scdc_dm_acceptance_test -v ON_ERROR_STOP=1
   if ($LASTEXITCODE -ne 0) { throw 'Scoped technical setup failed.' }
   $messages=@()
   foreach ($label in @('BIG-LOW','BIG-HIGH')) {
    $uuid=[guid]::NewGuid().ToString()
    $response=Invoke-DmRequest POST "/direct-conversations/$big/messages" @{clientMessageId=$uuid;content=$label} -AccessToken $session.accessToken
    Assert-DmStatus $response 200 'Send bigint fixture via writer'
    $messages+=@([pscustomobject]@{id=$response.Body.id;clientMessageId=$uuid;sequence=$response.Body.sequence;version=$response.Body.version;label=$label})
   }
   Write-DmJson $bigFile @{conversationId=$big;actorAlias=$ActorAlias;peerAlias='S13';messages=$messages}
  }
  $manifest.messages=@($manifest.messages | Where-Object { $_.conversationId -ne $space -and $_.conversationId -ne $big })
  foreach ($fixture in @($file,$bigFile)) {
   $data=Get-Content $fixture -Raw -Encoding UTF8 | ConvertFrom-Json
   foreach ($row in $data.messages) { $manifest.messages+=@([pscustomobject]@{conversationId=$data.conversationId;id=$row.id;sequence=$row.sequence;version=$row.version}) }
  }
  $manifest.conversations=@($manifest.conversations | Where-Object { $_.id -ne $space -and $_.id -ne $big })
  $manifest.conversations+=@([pscustomobject]@{id=$space;actorAlias=$ActorAlias;peerAlias='S12';fixture='H121'},[pscustomobject]@{id=$big;actorAlias=$ActorAlias;peerAlias='S13';fixture='bigint'})
  Write-DmJson (Join-Path $folder 'manifest.json') $manifest
  Write-Host "H121 actual writer: $space (121 messages); bigint actual writer: $big (2 messages)."
 } finally { $null=Invoke-DmRequest POST /auth/logout @{refreshToken=$session.refreshToken} }
 exit
}
$space=Resolve-Pair $PeerAlias
$reader=$ActorAlias; if ($ReaderAlias) { $reader=$ReaderAlias }
$session=$null
if (-not $Anonymous) { $session=Connect-DmActor $reader $Run 'DM-history-read' }
try {
 switch ($Action) {
  Page {
   $query="?limit=$Limit"
   foreach ($name in @('Before','After','Through')) {
    if ($PSBoundParameters.ContainsKey($name)) { $query+='&'+$name.ToLowerInvariant()+'='+[uri]::EscapeDataString((Get-Variable $name -ValueOnly)) }
   }
   $token=''; if ($null -ne $session) { $token=$session.accessToken }
   $response=Invoke-DmRequest GET "/direct-conversations/$space/messages$query" -AccessToken $token
   Assert-DmStatus $response $ExpectedStatus 'Read history'
   $response | ConvertTo-Json -Depth 10
  }
  Compare {
   $baselineJson=Read-Json $space; $baseline=Read-Ids $space; $pages=@(); $cursor=''; $ids=@(); $n=0
   do {
    $response=Read-Page $session $space $(if ($cursor) {'before'} else {''}) $cursor $Limit
    Assert-DmStatus $response 200 'History page'
    $page=$response.Body; $n++
    $current=@($page.items | ForEach-Object id); $ids=$current+$ids
    $pages+=@([pscustomobject]@{page=$n;count=@($page.items).Count;ids=$current;hasMore=$page.hasMore;throughSequence=$page.throughSequence})
    $cursor=$page.nextCursor
   } while ($cursor)
   if (($ids -join ',') -cne (($baseline | ForEach-Object id) -join ',') -or @($ids | Select-Object -Unique).Count -ne $ids.Count) { throw 'API/DB history mismatch or duplicate ID.' }
   if ((Read-Json $space) -cne $baselineJson) { throw 'History changed database messages.' }
   $proof=@{conversationId=$space;total=$ids.Count;pages=$pages;databaseIds=$baseline;result='PASS API/DB read-only'}
   Write-DmJson (Join-Path $folder "history-compare-$PeerAlias.json") $proof
   $proof | ConvertTo-Json -Depth 12
  }
  CursorChecks {
   $first=Read-Page $session $space '' '' 1; Assert-DmStatus $first 200 'Initial cursor'
   $cursor=$first.Body.nextCursor
   if (-not $cursor) { throw 'Need two messages for cursor checks.' }
   $other=Resolve-Pair 'S13'
   $cases=@(
    @{label='other-DM';path="/direct-conversations/$other/messages?limit=1&before="+[uri]::EscapeDataString($cursor);status=400;code='CURSOR_INVALID'},
    @{label='tampered';path="/direct-conversations/$space/messages?limit=1&before="+[uri]::EscapeDataString($cursor.Substring(0,$cursor.Length-8)+'invalid!');status=400;code='CURSOR_INVALID'},
    @{label='wrong-direction';path="/direct-conversations/$space/messages?limit=1&after="+[uri]::EscapeDataString($cursor);status=400;code='CURSOR_INVALID'},
    @{label='before-and-after';path="/direct-conversations/$space/messages?before=a&after=b";status=400;code='Common.ValidationFailed'}
   )
   $proof=@()
   foreach ($case in $cases) {
    $response=Invoke-DmRequest GET $case.path -AccessToken $session.accessToken
    Assert-DmStatus $response $case.status $case.label
    if ($response.Body.errorCode -cne $case.code) { throw 'Unexpected history errorCode.' }
    $proof+=@([pscustomobject]@{case=$case.label;status=$response.Status;errorCode=$response.Body.errorCode})
   }
   foreach ($alias in @('S12','C')) {
    $otherSession=Connect-DmActor $alias $Run 'DM-cursor-scope'
    try {
     $response=Read-Page $otherSession $space 'before' $cursor 1
     $expected=400; $code='CURSOR_INVALID'; if ($alias -eq 'C') { $expected=404; $code='RESOURCE_NOT_FOUND' }
     Assert-DmStatus $response $expected 'Current reader scope'
     if ($response.Body.errorCode -cne $code) { throw 'Unexpected reader scope errorCode.' }
     $proof+=@([pscustomobject]@{case="reader-$alias";status=$response.Status;errorCode=$response.Body.errorCode})
    } finally { $null=Invoke-DmRequest POST /auth/logout @{refreshToken=$otherSession.refreshToken} }
   }
   Write-DmJson (Join-Path $folder 'history-cursor-checks.json') $proof
   $proof | ConvertTo-Json -Depth 5
  }
  RestartProof {
   $first=Read-Page $session $space '' '' 1; Assert-DmStatus $first 200 'Before restart'
   $cursor=$first.Body.nextCursor
   if (-not $cursor) { throw 'Need two messages for protected before cursor.' }
   $oldPage=Read-Page $session $space 'before' $cursor 1; Assert-DmStatus $oldPage 200 'Old cursor'
   $baselineJson=Read-Json $space
   Invoke-DmCompose -Arguments @('restart','chat-service')
   $ready=$false
   for ($i=0;$i -lt 30;$i++) { try { $health=Invoke-DmRequest GET /health -ViaWeb; if ($health.Status -eq 200) { $ready=$true; break } } catch {} ; Start-Sleep -Milliseconds 500 }
   if (-not $ready) { throw 'API did not become ready.' }
   $newPage=Read-Page $session $space 'before' $cursor 1; Assert-DmStatus $newPage 200 'Protected cursor after restart'
   if (($oldPage.Body.items | ConvertTo-Json -Compress -Depth 10) -cne ($newPage.Body.items | ConvertTo-Json -Compress -Depth 10)) { throw 'Old cursor returned different DTOs.' }
   if ((Read-Json $space) -cne $baselineJson) { throw 'Restart changed messages.' }
   $proof=@{conversationId=$space;status=200;cursorPreserved=$true;messageIds=@($newPage.Body.items | ForEach-Object id);result='PASS'}
   Write-DmJson (Join-Path $folder 'history-restart-proof.json') $proof
   $proof | ConvertTo-Json
  }
 }
} finally { if ($null -ne $session) { $null=Invoke-DmRequest POST /auth/logout @{refreshToken=$session.refreshToken} } }
