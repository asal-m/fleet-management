param([string]$ProjectName = 'fleetmanagement-local', [int]$PostgresPort = 55432, [int]$RedisPort = 56379)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$fleetDocker = (Get-Command docker.exe).Source
$fleetDotnet = (Get-Command dotnet.exe).Source
Push-Location $projectRoot
try {
    # Keep generated credentials in memory and pass JSON through stdin, not arguments or logs.
    $configurationText = (& $fleetDocker compose -p $ProjectName exec -T application cat /run/fleet/appsettings.local.json | Out-String)
    if ($LASTEXITCODE -ne 0) { throw 'Start docker compose up --build -d before preparing Rider.' }
    $configuration = $configurationText | ConvertFrom-Json
    $settings = @{
        'ConnectionStrings:PostgreSql' = $configuration.ConnectionStrings.PostgreSql.Replace('Host=postgres;', "Host=localhost;Port=$PostgresPort;")
        'ConnectionStrings:Redis' = $configuration.ConnectionStrings.Redis.Replace('redis:6379,', "localhost:$RedisPort,")
        'Security:Authority' = 'http://localhost:18180'
        'Security:Audiences:0' = 'fleet-management'
        'Security:RequireHttpsMetadata' = 'false'
        'Database:ApplyMigrations' = 'false'
        'Kestrel:Endpoints:Rest:Url' = 'http://localhost:8180'
        'Kestrel:Endpoints:Grpc:Url' = 'http://localhost:8181'
        'Transport:RestPort' = '8180'
        'Transport:GrpcPort' = '8181'
    }
    $settings | ConvertTo-Json | & $fleetDotnet user-secrets set --id fleetcompany-fleetmanagement-local-development | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Unable to save Rider development settings.' }
    Write-Host 'Development User Secrets updated for the API; existing keys listed by this script were replaced.'
    Write-Host 'Start the separate local identity fixture on 18180, then Debug the API in Development.'
    Write-Host 'Rider REST/OpenAPI: http://localhost:8180/openapi-ui; gRPC: localhost:8181.'
    Write-Host 'Existing Docker application remains on 8080/8081. Database and cache are shared.'
} finally {
    Pop-Location
}
