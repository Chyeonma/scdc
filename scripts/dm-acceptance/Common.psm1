Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:RepoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$script:RuntimeRoot = Join-Path $script:RepoRoot '.dm-acceptance'
$script:EnvFile = Join-Path $script:RepoRoot '.env.dm-test'
$script:ComposeFile = Join-Path $script:RepoRoot 'compose.dm-test.yaml'
$script:Project = 'scdc-dm-acceptance'
$script:Database = 'scdc_dm_acceptance_test'
$script:Utf8 = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Net.Http

function Get-DmPaths {
    [pscustomobject]@{ Repo=$script:RepoRoot; Runtime=$script:RuntimeRoot; Env=$script:EnvFile }
}

function Write-DmJson($Path, $Value) {
    [System.IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Depth 30), $script:Utf8)
}

function Get-DmConfig {
    if (-not (Test-Path -LiteralPath $script:EnvFile)) { throw 'Run -Action Init first.' }
    $values = @{}
    foreach ($line in [System.IO.File]::ReadAllLines($script:EnvFile)) {
        if ($line -match '^([A-Z_]+)=(.*)$') { $values[$Matches[1]]=$Matches[2] }
    }
    foreach ($key in 'DM_WEB_PORT','DM_API_PORT','DM_POSTGRES_PORT','DM_POSTGRES_PASSWORD','DM_IDENTITY_SIGNING_KEY') {
        if (-not $values.ContainsKey($key)) { throw "Missing local config: $key" }
    }
    return $values
}

function Initialize-DmConfig([int]$WebPort=15300, [int]$ApiPort=15026, [int]$PostgresPort=15432) {
    foreach ($folder in '','runs','keyrings/cursor','keyrings/hmac','backend-artifacts','e2e') {
        $null = New-Item -ItemType Directory -Path (Join-Path $script:RuntimeRoot $folder) -Force
    }
    if (-not (Test-Path -LiteralPath $script:EnvFile)) {
        $bytes=New-Object byte[] 48
        $rng=[System.Security.Cryptography.RandomNumberGenerator]::Create()
        try { $rng.GetBytes($bytes); $dbPassword=[Convert]::ToBase64String($bytes); $rng.GetBytes($bytes); $signingKey=[Convert]::ToBase64String($bytes) } finally { $rng.Dispose() }
        $text="DM_WEB_PORT=$WebPort`nDM_API_PORT=$ApiPort`nDM_POSTGRES_PORT=$PostgresPort`nDM_POSTGRES_PASSWORD=$dbPassword`nDM_IDENTITY_SIGNING_KEY=$signingKey`n"
        [System.IO.File]::WriteAllText($script:EnvFile,$text,$script:Utf8)
    }
    $hmac=Join-Path $script:RuntimeRoot 'keyrings/hmac/dm-local-v1.key'
    if (-not (Test-Path -LiteralPath $hmac)) {
        $bytes=New-Object byte[] 32; $rng=[System.Security.Cryptography.RandomNumberGenerator]::Create()
        try { $rng.GetBytes($bytes); [System.IO.File]::WriteAllBytes($hmac,$bytes) } finally { $rng.Dispose() }
    }
    $config=Get-DmConfig
    if ($config.DM_WEB_PORT -eq $config.DM_API_PORT -or $config.DM_WEB_PORT -eq $config.DM_POSTGRES_PORT -or $config.DM_API_PORT -eq $config.DM_POSTGRES_PORT) { throw 'Acceptance ports must differ.' }
    Write-Host 'Local config/key created or preserved; no secret printed. Active features depend on the checkout used to build the acceptance stack.'
}

function Invoke-DmCompose([string[]]$Arguments) {
    $null=Get-DmConfig
    Push-Location $script:RepoRoot
    try {
        & docker compose --project-name $script:Project --env-file $script:EnvFile --file $script:ComposeFile @Arguments
        if ($LASTEXITCODE -ne 0) { throw "Compose command failed (exit $LASTEXITCODE)." }
    } finally { Pop-Location }
}

function Get-DmUrls {
    $config=Get-DmConfig
    [pscustomobject]@{ Web="http://localhost:$($config.DM_WEB_PORT)"; Api="http://localhost:$($config.DM_API_PORT)/api/v1"; Swagger="http://localhost:$($config.DM_API_PORT)/swagger"; PostgresPort=[int]$config.DM_POSTGRES_PORT }
}

function Invoke-DmRequest([string]$Method,[string]$Path,$Body=$null,[string]$AccessToken='',[switch]$ViaWeb) {
    $urls=Get-DmUrls
    $base=$urls.Api
    if ($ViaWeb) { $base=$urls.Web+'/api/v1' }
    $client=[System.Net.Http.HttpClient]::new()
    $client.Timeout=[TimeSpan]::FromSeconds(30)
    $request=[System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Method), $base+$Path)
    if ($AccessToken) { $request.Headers.Authorization=[System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer',$AccessToken) }
    if ($null -ne $Body) { $request.Content=[System.Net.Http.StringContent]::new(($Body | ConvertTo-Json -Depth 20),[System.Text.Encoding]::UTF8,'application/json') }
    try {
        $response=$client.SendAsync($request).GetAwaiter().GetResult()
        try {
            $raw=$response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            $parsed=$null
            if ($raw) { try { $parsed=$raw | ConvertFrom-Json } catch { throw 'Response is not JSON; inspect service status without printing sensitive bodies.' } }
            return [pscustomobject]@{ Status=[int]$response.StatusCode; Body=$parsed }
        } finally { $response.Dispose() }
    } finally { $request.Dispose(); $client.Dispose() }
}

function Assert-DmStatus($Response,[int]$Expected,[string]$Operation) {
    if ($Response.Status -ne $Expected) { throw "$Operation expected HTTP $Expected, actual HTTP $($Response.Status). Response suppressed to protect tokens." }
}

function Invoke-DmReadSql([string]$Sql) {
    # A read-only transaction enforces safety even if a caller supplies write SQL.
    $null=Get-DmConfig
    $command="BEGIN READ ONLY;`n$Sql`nCOMMIT;"
    Push-Location $script:RepoRoot
    try {
        $lines=$command | & docker compose --project-name $script:Project --env-file $script:EnvFile --file $script:ComposeFile exec -T postgres psql -X -q -A -t -v ON_ERROR_STOP=1 -U scdc_dm_test -d $script:Database
        if ($LASTEXITCODE -ne 0) { throw 'Read-only acceptance SQL failed.' }
        return ($lines -join "`n")
    } finally { Pop-Location }
}

function Assert-DmDatabase {
    $name=Invoke-DmReadSql 'SELECT current_database();'
    if ($name.Trim() -ne $script:Database) { throw 'Wrong database; stopping acceptance helper.' }
    $schema=Invoke-DmReadSql "SELECT count(*) FROM information_schema.tables WHERE table_schema='identity' AND table_name IN ('users','auth_sessions','user_emails','user_profiles');"
    if ($schema.Trim() -ne '4') { throw 'Identity schema is missing; do not replay destructive schema.sql on an existing volume.' }
}

function Assert-DmApiTarget {
    $config=Get-DmConfig
    $id=(Invoke-DmCompose -Arguments @('ps','-q','chat-service') | Out-String).Trim()
    if (-not $id) { throw 'Acceptance API container is not running.' }
    $envJson=& docker inspect --format '{{json .Config.Env}}' $id
    if ($LASTEXITCODE -ne 0) { throw 'Cannot verify acceptance API target.' }
    $entries=$envJson | ConvertFrom-Json
    $expected="ConnectionStrings__Database=Host=postgres;Port=5432;Database=$script:Database;Username=scdc_dm_test;Password=$($config.DM_POSTGRES_PASSWORD)"
    if ($entries -cnotcontains $expected -or $entries -cnotcontains 'ASPNETCORE_ENVIRONMENT=Development') { throw 'API is not the isolated Development acceptance database.' }
    $portJson=& docker inspect --format '{{json .NetworkSettings.Ports}}' $id
    if ($LASTEXITCODE -ne 0) { throw 'Cannot verify acceptance API port.' }
    $ports=$portJson | ConvertFrom-Json
    if (@($ports.'8080/tcp' | Where-Object { $_.HostPort -eq $config.DM_API_PORT -and $_.HostIp -eq '127.0.0.1' }).Count -ne 1) { throw 'Configured API port does not belong to the isolated acceptance container.' }
}

function Get-DmFixtureAccounts {
    $plan=Get-Content -LiteralPath (Join-Path $script:RepoRoot 'docs/fixtures/dm-demo-plan.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $accounts=@($plan.accounts)
    for ($i=1;$i -le 23;$i++) {
        $index=$i.ToString('00')
        $accounts += [pscustomobject]@{alias="S$index";username="dm_demo_search$index";email="dm-search$index@example.test";displayName="Người tìm $index";initialState='active_verified'}
    }
    return $accounts
}

function Get-DmRunPath([string]$Run) {
    if ($Run -notmatch '^[a-z0-9_-]{1,24}$') { throw 'Run must use 1-24 lowercase ASCII letters/digits/underscore/hyphen.' }
    $folder=Join-Path $script:RuntimeRoot "runs/$Run"
    $null=New-Item -ItemType Directory -Path $folder -Force
    return $folder
}

function Get-DmManifest([string]$Run='baseline') {
    $path=Join-Path (Get-DmRunPath $Run) 'manifest.json'
    if (-not (Test-Path -LiteralPath $path)) { throw 'Run -Action Setup for this run first.' }
    return (Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json)
}

function Get-DmUserSnapshot([string]$Username) {
    if ($Username -notmatch '^[a-z0-9_.]{3,32}$') { throw 'Invalid fixture username.' }
    $sql="SELECT row_to_json(s) FROM (SELECT u.id,u.username,u.status,p.display_name,e.email,e.verified_at,(SELECT count(*) FROM identity.auth_sessions a WHERE a.user_id=u.id AND a.revoked_at IS NULL AND a.expires_at>now()) active_sessions FROM identity.users u JOIN identity.user_profiles p ON p.user_id=u.id JOIN identity.user_emails e ON e.user_id=u.id AND e.is_primary WHERE u.normalized_username='$Username') s;"
    $raw=Invoke-DmReadSql $sql
    if (-not $raw.Trim()) { return $null }
    return ($raw | ConvertFrom-Json)
}

function Set-DmFixtures([string]$Run='baseline') {
    Assert-DmDatabase
    Assert-DmApiTarget
    $urls=Get-DmUrls
    $health=Invoke-DmRequest GET /health
    Assert-DmStatus $health 200 'Health'
    # Scope this ownership exception to the authorized repository and this command.
    # Sandbox-created worktrees may have another owner when Docker runs as the user.
    $commit=& git -c "safe.directory=$script:RepoRoot" -C $script:RepoRoot rev-parse HEAD
    if ($LASTEXITCODE -ne 0 -or $commit -notmatch '^[0-9a-f]{40}$') { throw 'Cannot read source commit; fixture manifest has not been changed.' }
    $folder=Get-DmRunPath $Run; $manifestPath=Join-Path $folder 'manifest.json'
    $old=$null
    if (Test-Path -LiteralPath $manifestPath) { $old=Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json }
    $accounts=@(Get-DmFixtureAccounts)
    $resolved=@()
    $owned=@()
    if ($null -ne $old) { $owned=@($old.accounts) }
    foreach ($a in $accounts) {
        $username=$a.username; $email=$a.email
        if ($Run -ne 'baseline') { $suffix='_'+$Run.Replace('-','_'); $username+=$suffix; $email=$a.email.Replace('@',"+$Run@") }
        if ($username.Length -gt 32) { throw 'Run suffix makes a username exceed 32 characters; choose a shorter run.' }
        $user=Get-DmUserSnapshot $username
        $known=@()
        if ($null -ne $old) { $known=@($old.accounts | Where-Object { $_.alias -eq $a.alias -and $_.username -eq $username }) }
        if ($null -ne $user) {
            if ($known.Count -ne 1 -or $known[0].id -ne $user.id -or $user.email -cne $email -or $user.display_name -cne $a.displayName) { throw "Existing account $username is not owned by this fixture manifest; no overwrite performed." }
            $want=0; if ($a.initialState -eq 'active_verified') { $want=1 }
            if ([int]$user.status -ne $want -or ($want -eq 1 -and $null -eq $user.verified_at) -or ($want -eq 0 -and $null -ne $user.verified_at)) { throw "Fixture state changed for $username; do not reset it automatically." }
        } else {
            if ($known.Count -gt 0) { throw "Manifest account $username disappeared; restore/check target instead of recreating IDs." }
            $registration=Invoke-DmRequest POST /auth/register @{username=$username;displayName=$a.displayName;email=$email;password='DmDemo2026!Local'}
            Assert-DmStatus $registration 201 "Register $($a.alias)"
            $resolved += [pscustomobject]@{alias=$a.alias;id=$registration.Body.userId;username=$username;email=$email;displayName=$a.displayName;state='registration-incomplete'}
            # Save ownership immediately; a partial setup is visible, never silently adopted.
            $owned=@($owned | Where-Object { $_.alias -ne $a.alias }) + @($resolved[-1])
            Write-DmJson $manifestPath @{run=$Run;database=$script:Database;status='setup-incomplete';accounts=$owned}
            if ($a.initialState -eq 'active_verified') {
                $token=$registration.Body.developmentVerificationToken
                if (-not $token) { throw 'Development verification token absent; enable only in the isolated Development stack.' }
                $verification=Invoke-DmRequest POST /auth/verify-email @{token=$token}
                Assert-DmStatus $verification 204 "Verify $($a.alias)"
                $token=$null
            }
            $user=Get-DmUserSnapshot $username
            $resolved=@($resolved | Where-Object { $_.alias -ne $a.alias })
        }
        $resolved += [pscustomobject]@{alias=$a.alias;id=$user.id;username=$username;email=$email;displayName=$a.displayName;state=$a.initialState}
        $owned=@($owned | Where-Object { $_.alias -ne $a.alias }) + @($resolved[-1])
        Write-DmJson $manifestPath @{run=$Run;database=$script:Database;status='setup-incomplete';accounts=$owned}
    }
    $manifest=[ordered]@{run=$Run;database=$script:Database;status='created-and-verified-via-real-identity-api';webUrl=$urls.Web;apiUrl=$urls.Api;sourceCommit=$commit.Trim();schemaSha256=(Get-FileHash (Join-Path $script:RepoRoot 'database/postgres/schema.sql') -Algorithm SHA256).Hash;createdAtUtc=[DateTime]::UtcNow.ToString('o');accounts=$resolved;conversations=@();messages=@()}
    Write-DmJson $manifestPath $manifest
    Write-Host "Fixture ${Run}: 28 accounts, 27 verified, U pending; no DM seeded. Manifest: $manifestPath"
}

function Connect-DmActor([string]$Alias,[string]$Run='baseline',[string]$DeviceName='DM-manual') {
    $manifest=Get-DmManifest $Run
    $actor=@($manifest.accounts | Where-Object { $_.alias -eq $Alias })
    if ($actor.Count -ne 1) { throw 'Alias missing from run manifest.' }
    $response=Invoke-DmRequest POST /auth/login @{login=$actor[0].username;password='DmDemo2026!Local';deviceName=$DeviceName}
    Assert-DmStatus $response 200 "Login $Alias"
    return $response.Body
}

Export-ModuleMember -Function *-Dm*
