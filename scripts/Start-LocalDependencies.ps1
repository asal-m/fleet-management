param([switch]$RunTests, [switch]$PostgreSqlOnly, [switch]$NoBuild,
    [string]$ProjectName = 'fleetmanagement-dependencies')
$ErrorActionPreference = 'Stop'
if ($PostgreSqlOnly) { throw 'The complete local workflow requires PostgreSQL, Redis and the development identity fixture. Use the LocalDevelopment tool.' }
$dotnetCommand = (Get-Command dotnet).Source
$mode = if ($RunTests) { 'verify' } else { 'run' }
$arguments = @('run', '--project', (Join-Path (Split-Path -Parent $PSScriptRoot) 'tools/LocalDevelopment'),
    '--configuration', 'Release', '--', $mode, '--project', $ProjectName)
if ($NoBuild) { $arguments += '--no-build' }
& $dotnetCommand @arguments
if ($LASTEXITCODE -ne 0) { throw "Local development exited with code $LASTEXITCODE." }
