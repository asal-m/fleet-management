param([switch]$RunTests, [switch]$PostgreSqlOnly)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$secretId = 'fleetcompany-fleetmanagement-local-development'
$dotnetCommand = (Get-Command dotnet.exe).Source
$dockerCommand = (Get-Command docker.exe).Source
$previousEnvironment = @{}
foreach ($name in @('FLEET_POSTGRES_USER', 'FLEET_POSTGRES_PASSWORD', 'FLEET_REDIS_PASSWORD',
    'FLEET_TEST_POSTGRES_CONNECTION', 'FLEET_TEST_REDIS_CONNECTION', 'ConnectionStrings__PostgreSql')) {
    $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}

# Capture secrets in memory. Never print the list or store a .env file in the repository.
$rawSecrets = (& $dotnetCommand user-secrets list --id $secretId --json | Out-String)
if ($LASTEXITCODE -ne 0) { throw 'Unable to read local user secrets.' }
$jsonSecrets = [regex]::Replace($rawSecrets, '(?m)^\s*//.*$', '').Trim()
$settings = if ($jsonSecrets) { $jsonSecrets | ConvertFrom-Json } else { [pscustomobject]@{} }

function Get-OrCreateLocalSecret([string]$key) {
    $property = $settings.PSObject.Properties[$key]
    if ($property -and $property.Value) { return [string]$property.Value }
    $bytes = New-Object byte[] 32
    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $generator.GetBytes($bytes) } finally { $generator.Dispose() }
    $value = [Convert]::ToBase64String($bytes)
    & $dotnetCommand user-secrets set $key $value --id $secretId | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Unable to save local user secrets.' }
    return $value
}

$userProperty = $settings.PSObject.Properties['LocalDevelopment:PostgreSqlUser']
if ($userProperty -and $userProperty.Value) {
    $env:FLEET_POSTGRES_USER = [string]$userProperty.Value
} else {
    $env:FLEET_POSTGRES_USER = 'fleet_' + [Guid]::NewGuid().ToString('N').Substring(0, 12)
    & $dotnetCommand user-secrets set 'LocalDevelopment:PostgreSqlUser' $env:FLEET_POSTGRES_USER --id $secretId | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Unable to save local database user.' }
}
$env:FLEET_POSTGRES_PASSWORD = Get-OrCreateLocalSecret 'LocalDevelopment:PostgreSqlPassword'
$env:FLEET_REDIS_PASSWORD = Get-OrCreateLocalSecret 'LocalDevelopment:RedisPassword'
$env:ConnectionStrings__PostgreSql = 'Host=localhost;Port=55432;Database=fleet_management;Username=' + $env:FLEET_POSTGRES_USER + ';Password=' + $env:FLEET_POSTGRES_PASSWORD
$redisConnection = 'localhost:56379,password=' + $env:FLEET_REDIS_PASSWORD
& $dotnetCommand user-secrets set 'ConnectionStrings:PostgreSql' $env:ConnectionStrings__PostgreSql --id $secretId | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Unable to save database connection.' }
& $dotnetCommand user-secrets set 'ConnectionStrings:Redis' $redisConnection --id $secretId | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Unable to save cache connection.' }

Push-Location $projectRoot
try {
    $composeArguments = @('compose', '-f', 'docker-compose.dependencies.yml', 'up', '-d', '--wait', '--wait-timeout', '120')
    if ($PostgreSqlOnly) { $composeArguments += 'postgres' }
    & $dockerCommand @composeArguments
    if ($LASTEXITCODE -ne 0) { throw 'Dependency startup failed.' }
    & $dotnetCommand ef database update --project src/FleetCompany.FleetManagement.Infrastructure --startup-project src/FleetCompany.FleetManagement.Api --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'Migration application failed.' }
    if ($RunTests) {
        $env:FLEET_TEST_POSTGRES_CONNECTION = $env:ConnectionStrings__PostgreSql
        if (-not $PostgreSqlOnly) { $env:FLEET_TEST_REDIS_CONNECTION = $redisConnection }
        else { $env:FLEET_TEST_REDIS_CONNECTION = $null }
        & $dotnetCommand test FleetCompany.FleetManagement.Backend.sln --configuration Release --verbosity minimal
        if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
    }
    if ($PostgreSqlOnly) { Write-Host 'Fleet PostgreSQL is ready; Redis was not started in this run.' }
    else { Write-Host 'Fleet PostgreSQL and Redis are ready.' }
    Write-Host 'Connection settings are stored in user secrets.'
    Write-Host 'OIDC authority and audience are still required before running the API.'
} finally {
    foreach ($name in $previousEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process')
    }
    Pop-Location
}
