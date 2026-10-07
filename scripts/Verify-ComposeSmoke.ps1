param([string]$ProjectName = 'fleetmanagement-local', [int]$RestPort = 8080,
    [int]$GrpcPort = 8081, [int]$IdentityPort = 55480, [int]$PostgresPort = 55432)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$fleetDocker = (Get-Command docker.exe).Source
$fleetDotnet = (Get-Command dotnet.exe).Source
$names = @('FLEET_SMOKE_REST_URI', 'FLEET_SMOKE_GRPC_URI', 'FLEET_SMOKE_IDENTITY_URI', 'FLEET_SMOKE_POSTGRES_PORT', 'FLEET_CONTAINER_POSTGRES_CONNECTION')
$previousEnvironment = @{}
foreach ($name in $names) { $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
Push-Location $projectRoot
try {
    $configurationText = (& $fleetDocker compose -p $ProjectName exec -T application cat /run/fleet/appsettings.local.json | Out-String)
    if ($LASTEXITCODE -ne 0) { throw 'Start docker compose up --build before smoke verification.' }
    $configuration = $configurationText | ConvertFrom-Json
    $env:FLEET_CONTAINER_POSTGRES_CONNECTION = $configuration.ConnectionStrings.PostgreSql
    $env:FLEET_SMOKE_REST_URI = "http://localhost:$RestPort"
    $env:FLEET_SMOKE_GRPC_URI = "http://localhost:$GrpcPort"
    $env:FLEET_SMOKE_IDENTITY_URI = "http://localhost:$IdentityPort"
    $env:FLEET_SMOKE_POSTGRES_PORT = [string]$PostgresPort
    & $fleetDotnet run --project tools/SmokeClient/SmokeClient.csproj --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'Socket smoke verification failed.' }
} finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process') }
    Pop-Location
}
