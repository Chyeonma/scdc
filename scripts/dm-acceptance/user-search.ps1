[CmdletBinding()]
param([ValidateSet('Upgrade','Smoke','Persistence')][string]$Action='Smoke', [string]$Run='baseline', [switch]$NoBuild)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force -DisableNameChecking
Assert-DmDatabase

if ($Action -eq 'Upgrade') {
    Assert-DmApiTarget
    if (-not $NoBuild) { Invoke-DmCompose -Arguments @('build','chat-service','web-client') }
    $sql=Get-Content (Join-Path (Get-DmPaths).Repo 'database/postgres/migrations/20261007_dm_p1_user_search.sql') -Raw -Encoding UTF8
    $sql | & docker compose --project-name scdc-dm-acceptance --env-file (Get-DmPaths).Env --file (Join-Path (Get-DmPaths).Repo 'compose.dm-test.yaml') exec -T postgres psql -X -v ON_ERROR_STOP=1 -U scdc_dm_test -d scdc_dm_acceptance_test
    if ($LASTEXITCODE -ne 0) { throw 'User-search additive migration failed.' }
    Invoke-DmCompose -Arguments @('run','--rm','--no-deps','chat-service','--initialize-user-search-keys')
    & (Join-Path (Get-DmPaths).Repo 'scripts/dm-acceptance.ps1') -Action Start -NoBuild
    # Reconcile once more after old writers have been replaced by the new API.
    Invoke-DmCompose -Arguments @('run','--rm','--no-deps','chat-service','--initialize-user-search-keys')
    if ((Invoke-DmReadSql "SELECT count(*) FROM identity.user_profiles WHERE display_name_search_key='';").Trim() -ne '0') { throw 'Search key backfill incomplete.' }
    Write-Host 'Search upgrade complete; IDs/text preserved, no schema.sql replay.'
    return
}

Assert-DmApiTarget
$manifest=Get-DmManifest $Run
$a=Connect-DmActor A $Run 'DM-P1-BE-A'
$b=Connect-DmActor B $Run 'DM-P1-BE-B'
$results=@()
function Search([string]$Q, [int]$Limit=20, [string]$Cursor, $Actor=$a) {
    $path='/users/search?q='+[Uri]::EscapeDataString($Q)+'&limit='+$Limit
    if ($PSBoundParameters.ContainsKey('Cursor')) { $path+='&cursor='+[Uri]::EscapeDataString($Cursor) }
    Invoke-DmRequest -Method GET -Path $path -AccessToken $Actor.accessToken
}
function Expect($Response, [int]$Status, [string]$Code='') {
    Assert-DmStatus $Response $Status 'Search'
    if ($Code -and $Response.body.errorCode -ne $Code) { throw 'Unexpected search errorCode.' }
}
$countsSql='SELECT (SELECT count(*) FROM messaging.direct_conversations), (SELECT count(*) FROM messaging.space_members), (SELECT count(*) FROM messaging.messages);'
try {
    $before=Invoke-DmReadSql $countsSql
    $first=Search 'dm_demo_search'; Expect $first 200
    if (@($first.body.items).Count -ne 20 -or -not $first.body.nextCursor) { throw 'Expected 20 search items and next cursor.' }
    $next=Search 'dm_demo_search' -Cursor $first.body.nextCursor; Expect $next 200
    if (@($next.body.items).Count -ne 3 -or $null -ne $next.body.nextCursor) { throw 'Expected final 3 search items.' }
    $ids=@($first.body.items.id)+@($next.body.items.id)
    if (@($ids | Select-Object -Unique).Count -ne 23) { throw 'Search pagination duplicate/missing IDs.' }
    $results+=@{case='C02';status=200;pages=@(20,3);uniqueIds=23;result='PASS'}
    if ($Action -eq 'Persistence') {
        Invoke-DmCompose -Arguments @('restart','chat-service')
        $ready=$false
        for ($i=0;$i -lt 45;$i++) { try { $h=Invoke-DmRequest GET /health; if ($h.Status -eq 200) { $ready=$true; break } } catch {}; Start-Sleep -Seconds 2 }
        if (-not $ready) { throw 'API restart timeout.' }
        $resumed=Search 'dm_demo_search' -Cursor $first.body.nextCursor; Expect $resumed 200
        if (($resumed.body.items.id -join ',') -ne ($next.body.items.id -join ',')) { throw 'Cursor did not survive API restart.' }
        $results+=@{case='persistent-cursor';sameSessionAndCursor=$true;items=3;result='PASS'}
    } else {
        $names=Search 'Bảo'; Expect $names 200
        foreach ($alias in 'B','C') { $expected=($manifest.accounts | Where-Object alias -eq $alias).id; if ($names.body.items.id -notcontains $expected) { throw 'Missing B/C.' } }
        foreach ($item in $names.body.items) { if (($item.PSObject.Properties.Name | Sort-Object) -join ',' -ne 'displayName,id,username') { throw 'Private field in search projection.' } }
        $exact=Search 'DM_DEMO_BAO'; Expect $exact 200
        if ($exact.body.items[0].id -ne ($manifest.accounts | Where-Object alias -eq 'B').id) { throw 'Exact username rank failed.' }
        foreach ($query in 'dm_demo_an','dm_demo_pending','zzz_no_dm_person') { $r=Search $query; Expect $r 200; if (@($r.body.items).Count -ne 0) { throw 'Self/pending/empty filter failed.' } }
        $results+=@{case='C01';duplicateNames=@('dm_demo_bao','dm_demo_chi');publicFields=@('id','username','displayName');selfAndPendingExcluded=$true;result='PASS'}
        foreach ($query in 'x',('a'*65),([string][char]::ConvertFromUtf32(0x1F600)*33)) { $r=Search $query; Expect $r 400 'Common.ValidationFailed' }
        foreach ($query in 'dm',('a'*64),([string][char]::ConvertFromUtf32(0x1F600)*32),'BẢO',('Ba'+[char]0x0309+'o'),'Bao','%%','__','\\') { $r=Search $query; Expect $r 200 }
        foreach ($query in '%%','\\','zzz_no_dm_person') { $r=Search $query; if (@($r.body.items).Count -ne 0) { throw 'Literal wildcard/empty query failed.' } }
        $results+=@{case='C03';utf16Boundaries=@(1,2,64,65);literalWildcards=$true;unicodeRequests=$true;result='PASS'}
        foreach ($r in @((Search 'Bảo' -Cursor $first.body.nextCursor),(Search 'dm_demo_search' -Limit 10 -Cursor $first.body.nextCursor),(Search 'dm_demo_search' -Cursor $first.body.nextCursor -Actor $b),(Search 'dm_demo_search' -Cursor 'tampered'))) { Expect $r 400 'CURSOR_INVALID' }
        foreach ($limit in 0,51) { $r=Search 'dm_demo_search' -Limit $limit; Expect $r 400 'Common.ValidationFailed' }
        $anon=Invoke-DmRequest GET '/users/search?q=dm_demo_search'; Expect $anon 401 'Common.Unauthorized'
        $results+=@{case='C04';changedQueryLimitActorAndTamperedRejected=$true;anonymous=401;result='PASS'}
    }
    $after=Invoke-DmReadSql $countsSql
    if ($before -cne $after) { throw 'Search changed Messaging counts.' }
    $folder=Get-DmRunPath $Run
    Write-DmJson (Join-Path $folder ('p1-search-'+$Action.ToLowerInvariant()+'.json')) @{task='DM-P1-T01';utc=[DateTime]::UtcNow.ToString('o');run=$Run;proof='real-HTTP-and-PostgreSQL';messagingCountsBefore=$before;messagingCountsAfter=$after;results=$results;userResult='Chưa xác nhận'}
    Write-Host "P1 search $Action PASS: actual HTTP/PostgreSQL; user result pending."
} finally {
    foreach ($s in $a,$b) { $null=Invoke-DmRequest POST /auth/logout @{refreshToken=$s.refreshToken} }
}
