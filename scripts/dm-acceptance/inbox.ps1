[CmdletBinding()]
param(
 [Parameter(Mandatory)][ValidateSet('Upgrade','Fixtures','Page','Compare','Smoke','Persistence','FaultOn','FaultOff','FaultProbe')][string]$Action,
 [string]$Run='baseline', [string]$ActorAlias='A', [ValidateRange(1,50)][int]$Limit=20, [string]$Cursor
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force -DisableNameChecking
Assert-DmDatabase
$paths=Get-DmPaths
$faultFile=Join-Path $paths.Runtime 'inbox-authority-fault.yaml'

function Wait-Api {
 $until=[DateTime]::UtcNow.AddSeconds(30)
 do {
  try { $r=Invoke-DmRequest GET /health; if ($r.Status -eq 200) { return } } catch { }
  Start-Sleep -Milliseconds 500
 } while ([DateTime]::UtcNow -lt $until)
 throw 'Acceptance API did not become ready.'
}
function Set-AuthorityFault {
 Assert-DmApiTarget
 # Override this acceptance API only. No credentials or production config in the override.
 $yaml="services:`n  chat-service:`n    environment:`n      ConnectionStrings__Database: Host=127.0.0.1;Port=1;Database=scdc_dm_acceptance_test;Username=scdc_dm_test;Password=unused;Timeout=1`n"
 [IO.File]::WriteAllText($faultFile,$yaml,[Text.UTF8Encoding]::new($false))
 & docker compose --project-name scdc-dm-acceptance --env-file $paths.Env -f (Join-Path $paths.Repo 'compose.dm-test.yaml') -f $faultFile up -d --no-deps --force-recreate chat-service
 if ($LASTEXITCODE -ne 0) { throw 'Fault API recreation failed. Run FaultOff.' }
 Wait-Api
}
function Clear-AuthorityFault {
 Invoke-DmCompose -Arguments @('up','-d','--no-deps','--force-recreate','chat-service')
 Wait-Api
 Assert-DmApiTarget
}
if ($Action -eq 'FaultOff') { Clear-AuthorityFault; Write-Host 'Acceptance API connection restored; DB and keys preserved.'; exit }
Assert-DmApiTarget
if ($Action -eq 'FaultOn') { Set-AuthorityFault; Write-Host 'Acceptance API authority fault enabled. Run FaultOff in finally.'; exit }
if ($Action -eq 'Upgrade') {
 foreach ($name in '20261009_dm_p1_open_conversation.sql','20261009_dm_p1_inbox.sql') {
  Get-Content (Join-Path $paths.Repo "database/postgres/migrations/$name") -Raw -Encoding UTF8 |
   & docker compose --project-name scdc-dm-acceptance --env-file $paths.Env -f (Join-Path $paths.Repo 'compose.dm-test.yaml') exec -T postgres psql -X -q -U scdc_dm_test -d scdc_dm_acceptance_test -v ON_ERROR_STOP=1
  if ($LASTEXITCODE -ne 0) { throw 'Additive migration failed.' }
 }
 Invoke-DmCompose -Arguments @('build','chat-service','web-client')
 & (Join-Path $PSScriptRoot '../dm-acceptance.ps1') -Action Start -NoBuild
 exit
}
$manifest=Get-DmManifest $Run
function Account([string]$Alias) {
 $found=@($manifest.accounts | Where-Object alias -eq $Alias)
 if ($found.Count -ne 1) { throw "Unknown fixture alias $Alias" }
 return $found[0]
}
function Page($Session,[int]$Size=20,[string]$After) {
 $query="/direct-conversations?limit=$Size"
 if ($After) { $query+='&cursor='+[Uri]::EscapeDataString($After) }
 return (Invoke-DmRequest GET $query -AccessToken $Session.accessToken)
}
function Test-InboxMembership($Session,[string]$Alias) {
 $ids=@(); $sizes=@(); $next=$null
 do {
  $r=Page $Session 20 $next; Assert-DmStatus $r 200 'Inbox page'
  $ids+=@($r.Body.items | ForEach-Object id); $sizes+=@($r.Body.items).Count; $next=$r.Body.nextCursor
 } while ($next)
 $actorId=([guid](Account $Alias).id).ToString()
 $sql=@"
SELECT coalesce(json_agg(s.id ORDER BY (s.last_activity_at IS NOT NULL) DESC,s.last_activity_at DESC,s.id),'[]')::text
FROM messaging.space_members m JOIN messaging.spaces s ON s.id=m.space_id
JOIN messaging.direct_conversations d ON d.space_id=s.id
WHERE m.user_id='$actorId' AND m.membership_status=1 AND m.left_at IS NULL
AND s.space_type=1 AND s.status<>3 AND s.deleted_at IS NULL
AND '$actorId'::uuid IN (d.user_low_id,d.user_high_id);
"@
 $db=(Invoke-DmReadSql $sql | ConvertFrom-Json)
 if (($ids -join ',') -cne ($db -join ',') -or @($ids | Sort-Object -Unique).Count -ne $ids.Count) { throw 'API/DB inbox IDs or order differ.' }
 return [pscustomobject]@{actor=$Alias;pageSizes=$sizes;count=$ids.Count;ids=$ids;apiEqualsDb=$true}
}
$actor=Account $ActorAlias
$session=Connect-DmActor $ActorAlias $Run 'DM-P1-T03-acceptance'
try {
 switch ($Action) {
  Fixtures {
   if ($ActorAlias -ne 'A') { throw 'Fixtures creates A/B, A/C, A/S01-S23 only.' }
   $created=@()
   foreach ($alias in @('B','C')+@(1..23 | ForEach-Object { 'S'+$_.ToString('00') })) {
    $peer=Account $alias
    $r=Invoke-DmRequest POST /direct-conversations @{peerUserId=$peer.id} -AccessToken $session.accessToken
    Assert-DmStatus $r 200 "Fixture A/$alias"
    $created+=[pscustomobject]@{alias="D-A$alias";id=$r.Body.id;actorAlias='A';peerAlias=$alias}
    $manifest.conversations=@($manifest.conversations | Where-Object id -ne $r.Body.id)+@($created[-1])
    Write-DmJson (Join-Path (Get-DmRunPath $Run) 'manifest.json') $manifest
   }
   $result=Test-InboxMembership $session 'A'
   if ($result.count -ne 25) { throw 'A has additional conversations. Keep data; use a distinct fixture run for exact 20+5 case.' }
   $result | ConvertTo-Json -Depth 6
  }
  Page { $r=Page $session $Limit $Cursor; Assert-DmStatus $r 200 'Inbox'; $r | ConvertTo-Json -Depth 8 }
  Compare { Test-InboxMembership $session $ActorAlias | ConvertTo-Json -Depth 6 }
  Smoke {
   $before=Invoke-DmReadSql 'SELECT concat((SELECT count(*) FROM messaging.direct_conversations), '':'' ,(SELECT count(*) FROM messaging.space_members), '':'' ,(SELECT count(*) FROM messaging.messages));'
   $a=Test-InboxMembership $session 'A'
   foreach ($alias in 'B','C','K') {
    $other=Connect-DmActor $alias $Run 'DM-P1-T03-isolation'
    try { Test-InboxMembership $other $alias | ConvertTo-Json -Depth 6 }
    finally { $null=Invoke-DmRequest POST /auth/logout @{refreshToken=$other.refreshToken} }
   }
   $first=Page $session; Assert-DmStatus $first 200 'First page'
   $c=Connect-DmActor 'C' $Run 'DM-P1-T03-cross-cursor'
   try { $bad=Page $c 20 $first.Body.nextCursor; Assert-DmStatus $bad 400 'Cross-actor cursor'; if ($bad.Body.errorCode -ne 'CURSOR_INVALID') { throw 'Wrong cursor error.' } }
   finally { $null=Invoke-DmRequest POST /auth/logout @{refreshToken=$c.refreshToken} }
   $anon=Invoke-DmRequest GET /direct-conversations; Assert-DmStatus $anon 401 'Anonymous inbox'
   $after=Invoke-DmReadSql 'SELECT concat((SELECT count(*) FROM messaging.direct_conversations), '':'' ,(SELECT count(*) FROM messaging.space_members), '':'' ,(SELECT count(*) FROM messaging.messages));'
   if ($before -cne $after) { throw 'Reads changed messaging rows.' }
   $a | ConvertTo-Json -Depth 6; Write-Host "Smoke PASS; pairs:members:messages $after; cross-cursor400; anonymous401."
  }
  Persistence {
   $before=Test-InboxMembership $session $ActorAlias; $first=Page $session
   Invoke-DmCompose -Arguments @('restart','chat-service','web-client'); Wait-Api
   $after=Test-InboxMembership $session $ActorAlias
   if (($before.ids -join ',') -cne ($after.ids -join ',')) { throw 'Inbox changed after restart.' }
   if ($first.Body.nextCursor) { $r=Page $session 20 $first.Body.nextCursor; Assert-DmStatus $r 200 'Pre-restart cursor'; Write-Host "Pre-restart cursor usable; remaining items $(@($r.Body.items).Count)." }
   $after | ConvertTo-Json -Depth 6; Write-Host 'Inbox and cursor keys survived API/FE restart.'
  }
  FaultProbe {
   $before=Test-InboxMembership $session $ActorAlias
   try { Set-AuthorityFault; $r=Page $session; Assert-DmStatus $r 503 'Authority fault'; if ($r.Body.errorCode -ne 'AUTHORITY_UNAVAILABLE') { throw 'Wrong authority error.' }; Write-Host 'Real API fault: HTTP503 AUTHORITY_UNAVAILABLE.' }
   finally { Clear-AuthorityFault }
   $after=Test-InboxMembership $session $ActorAlias
   if (($before.ids -join ',') -cne ($after.ids -join ',')) { throw 'Fault changed inbox.' }
   Write-Host 'Recovery200; inbox IDs preserved.'
  }
 }
} finally { $null=Invoke-DmRequest POST /auth/logout @{refreshToken=$session.refreshToken} }
