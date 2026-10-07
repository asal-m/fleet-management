$ErrorActionPreference='Stop'
$fleetDotnet='C:\Program Files\dotnet\dotnet.exe'
if(-not(Test-Path $fleetDotnet)){$fleetDotnet='dotnet'}
$secretsOutput=& $fleetDotnet user-secrets list --id fleetcompany-fleetmanagement-local-development --json
if($LASTEXITCODE -ne 0){throw 'Initialize local secrets with Start-LocalDependencies.ps1 first.'}
$settings=(($secretsOutput|Where-Object{$_ -notmatch '^//'})-join "`n")|ConvertFrom-Json
$env:FLEET_POSTGRES_USER=$settings.'LocalDevelopment:PostgreSqlUser'
$env:FLEET_POSTGRES_PASSWORD=$settings.'LocalDevelopment:PostgreSqlPassword'
$env:FLEET_REDIS_PASSWORD=$settings.'LocalDevelopment:RedisPassword'
if(-not $env:FLEET_POSTGRES_USER -or -not $env:FLEET_POSTGRES_PASSWORD -or -not $env:FLEET_REDIS_PASSWORD){throw 'Run Start-LocalDependencies.ps1 to initialize local secrets.'}
$runtimePassword=$settings.'LocalDevelopment:RuntimePostgreSqlPassword'
if(-not $runtimePassword){
 $randomBytes=New-Object byte[] 32
 $randomGenerator=[System.Security.Cryptography.RandomNumberGenerator]::Create()
 try{$randomGenerator.GetBytes($randomBytes)}finally{$randomGenerator.Dispose()}
 $runtimePassword=[Convert]::ToBase64String($randomBytes)
 & $fleetDotnet user-secrets set 'LocalDevelopment:RuntimePostgreSqlPassword' $runtimePassword --id fleetcompany-fleetmanagement-local-development | Out-Null
 if($LASTEXITCODE -ne 0){throw 'Cannot initialize runtime database credentials.'}
}
$env:FLEET_MIGRATION_POSTGRES_CONNECTION='Host=postgres;Database=fleet_management;Username='+$env:FLEET_POSTGRES_USER+';Password='+$env:FLEET_POSTGRES_PASSWORD
$env:FLEET_RUNTIME_POSTGRES_PASSWORD=$runtimePassword
$env:FLEET_CONTAINER_POSTGRES_CONNECTION='Host=postgres;Database=fleet_management;Username='+$env:FLEET_POSTGRES_USER+'_runtime;Password='+$runtimePassword
$env:FLEET_CONTAINER_REDIS_CONNECTION='redis:6379,password='+$env:FLEET_REDIS_PASSWORD+',abortConnect=false'
$env:FLEET_IDENTITY_ISSUER='http://identity:8080'
$env:FLEET_AUDIENCE='fleet-management'
Write-Host 'Compose environment initialized for this terminal; credentials were not printed or written to the repository.'
