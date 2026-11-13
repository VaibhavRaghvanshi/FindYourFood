#requires -Version 7.0
[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('Catalog', 'Customer', 'Location', 'Discovery')][string[]]$Services)
. "$PSScriptRoot/Development.ps1"

$ports = @{ Catalog = 5105; Customer = 5106; Location = 5107; Discovery = 5108 }
$servicesToStart = @($Services | Select-Object -Unique)
if ($servicesToStart -contains 'Discovery') { $servicesToStart = @($servicesToStart + @('Catalog', 'Location') | Select-Object -Unique) }
$needsProvider = $servicesToStart -contains 'Catalog'
$needsAdministration = $servicesToStart -contains 'Location'
$names = @('frontend', 'identity', 'gateway')
if ($needsProvider) { $names += @('provider', 'organization', 'provider-frontend', 'admin-frontend') }
elseif ($needsAdministration) { $names += @('organization', 'admin-frontend') }
$names += @($servicesToStart | ForEach-Object { $_.ToLowerInvariant() })
foreach ($name in $names) {
  if (Get-DevelopmentProcess $name) { throw "$name is already running. Stop the current profile first." }
}

$keys = @($ports.Keys | ForEach-Object { "ConnectionStrings__$_" })
$keys += @('ConnectionStrings__Identity', 'ConnectionStrings__Organization',
  'ConnectionStrings__Experience', 'ConnectionStrings__Provider', 'Tokens__PrivateKeyPath')
$saved = @{}
foreach ($key in $keys) { $saved[$key] = [Environment]::GetEnvironmentVariable($key) }
$flags = @{}
foreach ($service in $servicesToStart) {
  $flag = "Gateway__Enable$service"
  $flags[$flag] = [Environment]::GetEnvironmentVariable($flag)
}
Push-Location $projectRoot
try {
  $env:ASPNETCORE_ENVIRONMENT = 'Development'
  $env:DOTNET_ENVIRONMENT = 'Development'
  $env:Tokens__PublicKeyPath = Join-Path $localState 'keys/identity-public.pem'
  if (!(Test-Path -LiteralPath $env:Tokens__PublicKeyPath)) {
    throw 'Run Initialize-IdentityDatabase.ps1 first.'
  }

  Invoke-Checked 'dotnet' @('build', 'FindYourFood.slnx', '-m:1', '/nodeReuse:false')
  foreach ($key in $keys) { [Environment]::SetEnvironmentVariable($key, $null) }
  foreach ($service in $servicesToStart) {
    $key = "ConnectionStrings__$service"
    $connection = $saved[$key]
    if (!$connection) {
      $connection = "Server=.\MSSQL22;Database=FindYourFood_${service}Db;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
    }
    [Environment]::SetEnvironmentVariable($key, $connection)
    try {
      $name = $service.ToLowerInvariant()
      $api = Join-Path $projectRoot "backend/services/FindYourFood.$service.Api/FindYourFood.$service.Api"
      $url = "http://localhost:$($ports[$service])"
      $process = Start-DevelopmentProcess $name (Get-Command dotnet).Source @("bin/Debug/net10.0/FindYourFood.$service.Api.dll", '--urls', $url) $api
    } finally {
      [Environment]::SetEnvironmentVariable($key, $null)
    }
    Wait-ForEndpoint "$url/health/ready" $process
    "$service is ready on $url."
    [Environment]::SetEnvironmentVariable("Gateway__Enable$service", 'true')
  }
  foreach ($key in $keys) {
    if ($key -notin @($ports.Keys | ForEach-Object { "ConnectionStrings__$_" })) {
      [Environment]::SetEnvironmentVariable($key, $saved[$key])
    }
  }
  if ($needsProvider) {
    & "$PSScriptRoot/Start-ProviderEnvironment.ps1" -SkipBuild
  } elseif ($needsAdministration) {
    & "$PSScriptRoot/Start-AdministrationEnvironment.ps1" -SkipBuild
  } else {
    & "$PSScriptRoot/Start-IdentityEnvironment.ps1" -SkipBuild
  }
  & "$PSScriptRoot/Start-Frontend.ps1"
} catch {
  foreach ($name in $names) { Stop-DevelopmentProcess $name }
  throw
} finally {
  foreach ($key in $keys) { [Environment]::SetEnvironmentVariable($key, $saved[$key]) }
  foreach ($flag in $flags.Keys) { [Environment]::SetEnvironmentVariable($flag, $flags[$flag]) }
  Pop-Location
}
