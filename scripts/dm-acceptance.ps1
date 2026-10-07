[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Init','Start','Stop','Restart','Status','Setup','Login','Smoke','Persistence','Db','BackendTests')]
    [string]$Action,
    [string]$Run='baseline',
    [string]$Alias='A',
    [string]$DeviceName='DM-manual',
    [int]$WebPort=15300,
    [int]$ApiPort=15026,
    [int]$PostgresPort=15432,
    [string]$SqlFile,
    [switch]$PassThru,
    [switch]$NoBuild
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'dm-acceptance/Common.psm1') -Force -DisableNameChecking

function Wait-DmReady {
    $ready=$false
    for ($i=0;$i -lt 90;$i++) {
        try {
            $health=Invoke-DmRequest GET /health
            $proxy=Invoke-DmRequest GET /health -ViaWeb
            if ($health.Status -eq 200 -and $proxy.Status -eq 200) { $ready=$true; break }
        } catch { }
        Start-Sleep -Seconds 2
    }
    if (-not $ready) { throw 'API/proxy did not become ready. Run Status; no fixture created.' }
    Assert-DmDatabase
    Assert-DmApiTarget
    Get-DmUrls
}

switch ($Action) {
    Init { Initialize-DmConfig $WebPort $ApiPort $PostgresPort }
    Start {
        Initialize-DmConfig $WebPort $ApiPort $PostgresPort
        $config=Get-DmConfig
        # Permit a port only when it belongs to this existing acceptance stack.
        $ids=@(Invoke-DmCompose -Arguments @('ps','-q'))
        $ownedPorts=@()
        foreach ($id in $ids) {
            if (-not $id) { continue }
            $json=& docker inspect --format '{{json .NetworkSettings.Ports}}' $id
            if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect existing acceptance ports.' }
            $bindings=$json | ConvertFrom-Json
            foreach ($property in $bindings.PSObject.Properties) { foreach ($binding in $property.Value) { if ($null -ne $binding) { $ownedPorts+=[int]$binding.HostPort } } }
        }
        foreach ($port in [int]$config.DM_WEB_PORT,[int]$config.DM_API_PORT,[int]$config.DM_POSTGRES_PORT) {
            $listener=[System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback,$port)
            try { $listener.Start() } catch { if ($ownedPorts -notcontains $port) { throw "Port $port is already occupied outside acceptance. Choose other ports before first Init; no process stopped." } } finally { $listener.Stop() }
        }
        $composeArgs=@('up','-d'); if (-not $NoBuild) { $composeArgs+='--build' }
        Invoke-DmCompose -Arguments ($composeArgs+@('postgres','chat-service','web-client'))
        Wait-DmReady
    }
    Stop { Invoke-DmCompose -Arguments @('stop','web-client','chat-service','postgres'); Write-Host 'Only acceptance services stopped; volumes and keys preserved.' }
    Restart { Invoke-DmCompose -Arguments @('restart','postgres','chat-service','web-client'); Wait-DmReady }
    Status { Invoke-DmCompose -Arguments @('ps'); Get-DmUrls }
    Setup { Set-DmFixtures $Run }
    Login {
        Assert-DmApiTarget
        $session=Connect-DmActor $Alias $Run $DeviceName
        if ($PassThru) { $session } else { $session.user | Select-Object id,username,displayName,emailVerified,status; Write-Host 'Tokens not printed; use -PassThru assigned to a variable for REST tests.' }
    }
    Db {
        Assert-DmDatabase
        if (-not $SqlFile) { $SqlFile=Join-Path $PSScriptRoot 'dm-acceptance/baseline-readonly.sql' }
        Invoke-DmReadSql (Get-Content -LiteralPath $SqlFile -Raw -Encoding UTF8)
    }
    BackendTests {
        Assert-DmDatabase
        # The profile uses its own PostgreSQL DB and redirects build output off the read-only source mount.
        Invoke-DmCompose -Arguments @('--profile','tools','run','--rm','backend-tests')
    }
    Persistence {
        Assert-DmDatabase; Assert-DmApiTarget
        $manifest=Get-DmManifest $Run
        $names=@($manifest.accounts | ForEach-Object { if ($_.username -notmatch '^[a-z0-9_.]{3,32}$') { throw 'Invalid manifest username.' }; "'$($_.username)'" }) -join ','
        $sql="SELECT coalesce(json_agg(s ORDER BY s.username),'[]')::text FROM (SELECT u.id,u.username,u.status,p.display_name,p.bio,e.email,e.verified_at FROM identity.users u JOIN identity.user_profiles p ON p.user_id=u.id JOIN identity.user_emails e ON e.user_id=u.id AND e.is_primary WHERE u.normalized_username IN ($names)) s;"
        $before=Invoke-DmReadSql $sql
        $records=$before | ConvertFrom-Json
        if ($records.Count -ne 28) { throw 'Before persistence check: fixture must have 28 records.' }
        Set-DmFixtures $Run
        Set-DmFixtures $Run
        Invoke-DmCompose -Arguments @('restart','postgres','chat-service','web-client')
        Wait-DmReady
        $after=Invoke-DmReadSql $sql
        if ($before -cne $after) { throw 'Fixture identity/profile/email changed after repeated setup/restart.' }
        $folder=Get-DmRunPath $Run
        Write-DmJson (Join-Path $folder 'persistence.json') @{case='DM-P0-T01-C04';run=$Run;utc=[DateTime]::UtcNow.ToString('o');result='PASS';proof='two-real-api-setup-runs-plus-compose-restart-and-postgresql-readback';accountCount=28;idsProfileAndEmailUnchanged=$true;userResult='Chưa xác nhận'}
        Write-Host 'C04 PASS: 28 IDs/profile/email unchanged after setup twice and restart; no volume deleted.'
    }
    Smoke {
        Assert-DmDatabase; Assert-DmApiTarget
        $manifest=Get-DmManifest $Run
        if (@($manifest.accounts).Count -ne 28) { throw 'Fixture manifest must contain 28 accounts.' }
        $results=@()
        foreach ($viaWeb in @($false,$true)) {
            $r=Invoke-DmRequest GET /health -ViaWeb:$viaWeb; Assert-DmStatus $r 200 'Health'
            $r=Invoke-DmRequest GET /users/me -ViaWeb:$viaWeb; Assert-DmStatus $r 401 'Anonymous GET me'
            $results += @{case='health-and-anonymous';viaWeb=$viaWeb;result='PASS';health=200;anonymous=401}
        }
        $a=Connect-DmActor A $Run 'DM-smoke-A'; $b=Connect-DmActor B $Run 'DM-smoke-B'; $c=Connect-DmActor C $Run 'DM-smoke-C'
        try {
            foreach ($session in $a,$b,$c) {
                $me=Invoke-DmRequest GET /users/me -AccessToken $session.accessToken
                Assert-DmStatus $me 200 'Authenticated GET me'
                if ($me.Body.id -ne $session.user.id) { throw 'Actor mismatch.' }
            }
            $u=@($manifest.accounts | Where-Object { $_.alias -eq 'U' })[0]
            $before=Get-DmUserSnapshot $u.username
            $pending=Invoke-DmRequest POST /auth/login @{login=$u.username;password='DmDemo2026!Local';deviceName='DM-smoke-U'}
            Assert-DmStatus $pending 403 'Pending login'
            if ($pending.Body.errorCode -ne 'Identity.EmailNotVerified') { throw 'Unexpected pending errorCode.' }
            $after=Get-DmUserSnapshot $u.username
            if ($before.active_sessions -ne $after.active_sessions -or $after.status -ne 0 -or $null -ne $after.verified_at) { throw 'Pending account/session invariant failed.' }
            $results += @{case='DM-P0-T01-C02';result='PASS';status=403;errorCode=$pending.Body.errorCode;activeSessionDelta=0}
            $profile=Invoke-DmRequest PATCH /users/me @{displayName=$a.user.displayName;bio='Kiểm tra hồ sơ DM-P0-T01-C01';locale='vi-VN';timezone='Asia/Ho_Chi_Minh'} -AccessToken $a.accessToken
            Assert-DmStatus $profile 200 'PATCH me'
            $me=Invoke-DmRequest GET /users/me -AccessToken $a.accessToken -ViaWeb
            Assert-DmStatus $me 200 'GET me via web'
            if ($me.Body.bio -cne 'Kiểm tra hồ sơ DM-P0-T01-C01') { throw 'Profile readback mismatch.' }
            $results += @{case='DM-P0-T01-C01';result='PASS';userId=$me.Body.id;profileStatus=200;proxyReadStatus=200}
            $logout=Invoke-DmRequest POST /auth/logout @{refreshToken=$a.refreshToken}; Assert-DmStatus $logout 204 'Logout A'
            $old=Invoke-DmRequest GET /users/me -AccessToken $a.accessToken; Assert-DmStatus $old 401 'Revoked A token'
            $stillB=Invoke-DmRequest GET /users/me -AccessToken $b.accessToken; Assert-DmStatus $stillB 200 'Independent B token'
            if ($stillB.Body.id -ne $b.user.id) { throw 'Logout changed B actor.' }
            $results += @{case='DM-P0-T01-C03';result='PASS';logout=204;oldA=401;independentB=200}
        } finally {
            foreach ($session in $b,$c) { $r=Invoke-DmRequest POST /auth/logout @{refreshToken=$session.refreshToken}; Assert-DmStatus $r 204 'Smoke cleanup logout' }
            # A may already be logged out: repeat logout is safe and does not print tokens.
            $r=Invoke-DmRequest POST /auth/logout @{refreshToken=$a.refreshToken}
        }
        $folder=Get-DmRunPath $Run
        Write-DmJson (Join-Path $folder 'backend-smoke.json') @{run=$Run;utc=[DateTime]::UtcNow.ToString('o');proof='real-http-and-postgresql';results=$results;userResult='Chưa xác nhận'}
        $results | ForEach-Object { [pscustomobject]$_ } | Format-Table -AutoSize
        Write-Host 'Backend smoke completed. FE browser and user acceptance remain separate.'
    }
}
