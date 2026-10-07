param([int]$RestPort = 58080, [int]$GrpcPort = 58081, [int]$IdentityPort = 58480,
    [int]$PostgresPort = 58432, [int]$RedisPort = 58379)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$fleetDocker = (Get-Command docker.exe).Source
$projectName = 'fleet-verification-' + [Guid]::NewGuid().ToString('N').Substring(0, 12)
$scratchDirectory = Join-Path $projectRoot '.scratch/compose-verification'
New-Item -ItemType Directory -Force -Path $scratchDirectory | Out-Null
$overrideFile = Join-Path $scratchDirectory "$projectName.yml"
@"
services:
  postgres:
    ports: !override ["127.0.0.1:${PostgresPort}:5432"]
  redis:
    ports: !override ["127.0.0.1:${RedisPort}:6379"]
  identity:
    ports: !override ["127.0.0.1:${IdentityPort}:8080"]
  application:
    ports: !override ["127.0.0.1:${RestPort}:8080", "127.0.0.1:${GrpcPort}:8081"]
"@ | Set-Content -LiteralPath $overrideFile -Encoding utf8
$compose = @('compose', '-p', $projectName, '-f', (Join-Path $projectRoot 'docker-compose.yml'), '-f', $overrideFile)
$importVariables = @('FLEET_POSTGRES_USER', 'FLEET_POSTGRES_PASSWORD', 'FLEET_REDIS_PASSWORD', 'FLEET_RUNTIME_POSTGRES_PASSWORD')
$previousEnvironment = @{}
foreach ($name in $importVariables) {
    $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
    [Environment]::SetEnvironmentVariable($name, $null, 'Process')
}
$created = $false
try {
    $containers = & $fleetDocker ps -aq --filter "label=com.docker.compose.project=$projectName"
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect Docker containers.' }
    $volumes = & $fleetDocker volume ls -q --filter "label=com.docker.compose.project=$projectName"
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect Docker volumes.' }
    if ($containers -or $volumes) { throw 'Verification requires an unused project name and empty volumes.' }
    Write-Host "Fresh verification project: $projectName (no existing containers or volumes)."
    & $fleetDocker @compose config --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Compose configuration is invalid.' }
    $created = $true
    & $fleetDocker @compose up --build -d
    if ($LASTEXITCODE -ne 0) { throw 'Fresh docker compose up --build failed.' }
    $ready = $false
    for ($attempt = 0; $attempt -lt 90; $attempt++) {
        try {
            $ready = (Invoke-RestMethod -Uri "http://localhost:$RestPort/health/ready" -TimeoutSec 3) -eq 'Healthy'
        } catch { $ready = $false }
        if ($ready) { break }
        Start-Sleep -Seconds 2
    }
    if (-not $ready) { throw 'Fresh application did not become ready.' }
    Write-Host 'PASS: Empty-volume Compose build, bootstrap, migrations and readiness.'
    & (Join-Path $PSScriptRoot 'Test-ComposeEnvironment.ps1') -ProjectName $projectName -PostgresPort $PostgresPort -RedisPort $RedisPort
    & (Join-Path $PSScriptRoot 'Verify-ComposeSmoke.ps1') -ProjectName $projectName -RestPort $RestPort -GrpcPort $GrpcPort -IdentityPort $IdentityPort -PostgresPort $PostgresPort
    # Run dependency failure only after the suite has finished.
    & (Join-Path $PSScriptRoot 'Verify-RedisFailure.ps1') -ProjectName $projectName -RestPort $RestPort -IdentityPort $IdentityPort
    Write-Host 'PASS: Fresh environment suite, live REST/gRPC/OIDC/Audit and Redis failure/recovery.'
} finally {
    try {
        # Only this generated, initially unused Compose project is removed.
        if ($created) {
            & $fleetDocker @compose down --volumes --remove-orphans
            if ($LASTEXITCODE -ne 0) { throw "Verification cleanup failed for $projectName." }
        }
    } finally {
        foreach ($name in $importVariables) { [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process') }
    }
}
