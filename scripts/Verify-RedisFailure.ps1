param([string]$ProjectName = 'fleetmanagement-local', [int]$RestPort = 8080, [int]$IdentityPort = 55480)

$ErrorActionPreference = 'Stop'
$fleetDocker = 'C:\Program Files\Docker\Docker\resources\bin\docker.exe'
if (-not (Test-Path -LiteralPath $fleetDocker)) { $fleetDocker = (Get-Command docker.exe).Source }
$baseUri = "http://localhost:$RestPort"
$tokenUri = "http://localhost:$IdentityPort/development/token"
$managerToken = (Invoke-RestMethod -Method Post -Uri $tokenUri -ContentType 'application/json' -Body '{"role":"FleetManager"}').access_token
$operatorToken = (Invoke-RestMethod -Method Post -Uri $tokenUri -ContentType 'application/json' -Body '{"role":"Operator"}').access_token
$managerHeaders = @{ Authorization = "Bearer $managerToken" }
$operatorHeaders = @{ Authorization = "Bearer $operatorToken" }
# The mutation creates a new revision: the first query during the outage cannot
# be satisfied by a previously warmed local-memory cache key.
$plate = 'REDIS-' + [Guid]::NewGuid().ToString('N').Substring(0,20)
$vehicle = Invoke-RestMethod -Method Post -Uri "$baseUri/api/fleet/vehicles/" -Headers $managerHeaders -ContentType 'application/json' -Body (@{plateNumber=$plate;typeCode='TRUCK';capacityKilograms=1000;baseStatus=1}|ConvertTo-Json)
$stopped = $false
try {
    & $fleetDocker compose -p $ProjectName -f (Join-Path (Split-Path -Parent $PSScriptRoot) 'docker-compose.yml') stop redis | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Unable to stop the Fleet Redis container.' }
    $stopped = $true
    $rows = Invoke-RestMethod -Uri "$baseUri/api/fleet/vehicles/available?limit=200" -Headers $operatorHeaders -TimeoutSec 45
    if (-not ($rows | Where-Object { $_.id -eq $vehicle.id })) { throw 'Fresh availability did not include the test vehicle during the Redis outage.' }
    $health = Invoke-RestMethod -Uri "$baseUri/health/ready" -TimeoutSec 10
    if ($health -ne 'Healthy') { throw 'An optional-cache outage incorrectly changed application readiness.' }
    Write-Host 'PASS: Fresh availability falls back to PostgreSQL while Redis is stopped.'
    Write-Host 'PASS: Application readiness stays healthy.'
} finally {
    if ($stopped) {
        & $fleetDocker compose -p $ProjectName -f (Join-Path (Split-Path -Parent $PSScriptRoot) 'docker-compose.yml') start redis | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Restarting Fleet Redis failed; run docker compose start redis manually.' }
    }
    Invoke-RestMethod -Method Put -Uri "$baseUri/api/fleet/vehicles/$($vehicle.id)/status" -Headers $managerHeaders -ContentType 'application/json' -Body '{"status":2}' | Out-Null
}
$recovered = Invoke-RestMethod -Uri "$baseUri/api/fleet/vehicles/available?limit=200" -Headers $operatorHeaders -TimeoutSec 45
if ($recovered | Where-Object { $_.id -eq $vehicle.id }) { throw 'Availability returned the vehicle after it became inactive.' }
Write-Host 'PASS: Redis restarted and availability reflects the new vehicle status.'
Write-Host 'The fictional test vehicle remains inactive; audit records are retained.'
