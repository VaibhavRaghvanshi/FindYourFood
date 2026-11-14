#requires -Version 7.0
[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('Catalog', 'Customer', 'Location', 'Discovery', 'Cart', 'Ordering', 'Payment')][string[]]$Services, [switch]$SkipBuild)
. "$PSScriptRoot/Development.ps1"
. "$PSScriptRoot/Messaging.ps1"

$ports = @{ Catalog = 5105; Customer = 5106; Location = 5107; Discovery = 5108; Cart = 5109; Ordering = 5110; Payment = 5111 }
$servicesToStart = @($Services | Select-Object -Unique)
$messaging = $servicesToStart -contains 'Payment'
if ($messaging) { $servicesToStart = @($servicesToStart + 'Ordering' | Select-Object -Unique) }
if ($servicesToStart -contains 'Ordering') { $servicesToStart = @($servicesToStart + 'Cart' | Select-Object -Unique) }
if ($servicesToStart -contains 'Discovery' -or $servicesToStart -contains 'Cart') {
  $servicesToStart = @($servicesToStart + @('Catalog', 'Location') | Select-Object -Unique)
}
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
$messagingKeys = @('Messaging__Enabled', 'Messaging__Host', 'Messaging__Port', 'Messaging__User',
  'Messaging__VirtualHost', 'Messaging__PasswordPath', 'Messaging__Prefix')
$keys += $messagingKeys
$saved = @{}
foreach ($key in $keys) { $saved[$key] = [Environment]::GetEnvironmentVariable($key) }
$flags = @{}
$flags['Gateway__ConstrainedWorkstation'] = $env:Gateway__ConstrainedWorkstation
foreach ($service in $servicesToStart) {
  $flag = "Gateway__Enable$service"
  $flags[$flag] = [Environment]::GetEnvironmentVariable($flag)
}
Push-Location $projectRoot
try {
  if (!$env:Gateway__ConstrainedWorkstation) { $env:Gateway__ConstrainedWorkstation = 'true' }
  if ($messaging -and !(Get-DevelopmentProcess 'rabbitmq')) {
    & "$PSScriptRoot/Start-MessagingBroker.ps1"
    $startedBroker = $true
  }
  $env:ASPNETCORE_ENVIRONMENT = 'Development'
  $env:DOTNET_ENVIRONMENT = 'Development'
  $env:Tokens__PublicKeyPath = Join-Path $localState 'keys/identity-public.pem'
  if (!(Test-Path -LiteralPath $env:Tokens__PublicKeyPath)) {
    throw 'Run Initialize-IdentityDatabase.ps1 first.'
  }

  if (!$SkipBuild) { Invoke-Checked 'dotnet' @('build', 'FindYourFood.slnx', '-m:1', '/nodeReuse:false') }
  foreach ($key in $keys) { [Environment]::SetEnvironmentVariable($key, $null) }
  foreach ($service in $servicesToStart) {
    $key = "ConnectionStrings__$service"
    $connection = $saved[$key]
    if (!$connection) {
      $connection = "Server=.\MSSQL22;Database=FindYourFood_${service}Db;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
    }
    [Environment]::SetEnvironmentVariable($key, $connection)
    try {
      if ($messaging -and $service -in @('Ordering', 'Payment')) {
        Set-MessagingDevelopmentConfiguration
        $env:Messaging__Prefix = $saved['Messaging__Prefix']
      }
      $name = $service.ToLowerInvariant()
      $api = Join-Path $projectRoot "backend/services/FindYourFood.$service.Api/FindYourFood.$service.Api"
      $url = "http://localhost:$($ports[$service])"
      $process = Start-DevelopmentProcess $name (Get-Command dotnet).Source @("bin/Debug/net10.0/FindYourFood.$service.Api.dll", '--urls', $url) $api
    } finally {
      [Environment]::SetEnvironmentVariable($key, $null)
      foreach ($messagingKey in $messagingKeys) { [Environment]::SetEnvironmentVariable($messagingKey, $null) }
    }
    Wait-ForEndpoint "$url/health/ready" $process
    "$service is ready on $url."
    [Environment]::SetEnvironmentVariable("Gateway__Enable$service", 'true')
  }
  foreach ($key in $keys) {
    if ($key -notin @($ports.Keys | ForEach-Object { "ConnectionStrings__$_" }) -and $key -notin $messagingKeys) {
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
  if ($startedBroker) { & "$PSScriptRoot/Stop-MessagingBroker.ps1" }
  throw
} finally {
  foreach ($key in $keys) { [Environment]::SetEnvironmentVariable($key, $saved[$key]) }
  foreach ($flag in $flags.Keys) { [Environment]::SetEnvironmentVariable($flag, $flags[$flag]) }
  Pop-Location
}
