param([string]$ProjectName = 'fleetmanagement-local', [int]$PostgresPort = 55432, [int]$RedisPort = 56379)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$fleetDocker = (Get-Command docker.exe).Source
$fleetDotnet = (Get-Command dotnet.exe).Source
$names = @('FLEET_TEST_POSTGRES_CONNECTION', 'FLEET_TEST_REDIS_CONNECTION', 'ConnectionStrings__PostgreSql', 'ConnectionStrings__Redis', 'LocalEnvironment__ConfigurationFile')
$previousEnvironment = @{}
foreach ($name in $names) { $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }

Push-Location $projectRoot
try {
    # Capture the mounted file in memory; no secret output or host-side credential file.
    $configurationText = (& $fleetDocker compose -p $ProjectName exec -T application cat /run/fleet/appsettings.local.json | Out-String)
    if ($LASTEXITCODE -ne 0) { throw 'Start docker compose up --build before testing.' }
    $configuration = $configurationText | ConvertFrom-Json
    # The migration owner is needed for fixture cleanup, while live application audit remains restricted.
    $env:FLEET_TEST_POSTGRES_CONNECTION = $configuration.ConnectionStrings.MigrationPostgreSql.Replace('Host=postgres;', "Host=localhost;Port=$PostgresPort;")
    $env:FLEET_TEST_REDIS_CONNECTION = $configuration.ConnectionStrings.Redis.Replace('redis:6379,', "localhost:$RedisPort,")
    $env:ConnectionStrings__PostgreSql = $env:FLEET_TEST_POSTGRES_CONNECTION
    $env:ConnectionStrings__Redis = $env:FLEET_TEST_REDIS_CONNECTION
    $env:LocalEnvironment__ConfigurationFile = $null
    & $fleetDotnet test FleetCompany.FleetManagement.Backend.sln --configuration Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw 'Automated tests failed.' }
} finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process') }
    Pop-Location
}
