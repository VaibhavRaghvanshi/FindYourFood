#requires -Version 7.0
. "$PSScriptRoot/Development.ps1"

$names = @('provider-frontend', 'admin-frontend', 'gateway', 'identity', 'organization', 'provider')
foreach ($name in $names) {
  if (Get-DevelopmentProcess $name) { throw "$name is already running. Stop the current profile first." }
}

$previousConnection = $env:ConnectionStrings__Provider
$previousFlag = $env:Gateway__EnableProvider
Push-Location $projectRoot
try {
  Set-ProviderDevelopmentConfiguration
  if (!(Test-Path -LiteralPath $env:Tokens__PublicKeyPath)) {
    throw 'Run Initialize-IdentityDatabase.ps1 to create the Identity public key first.'
  }
  Invoke-Checked 'dotnet' @('build', 'FindYourFood.slnx', '-m:1', '/nodeReuse:false')
  $privateSettings = @('ConnectionStrings__Identity', 'ConnectionStrings__Organization',
    'ConnectionStrings__Experience', 'Tokens__PrivateKeyPath')
  $saved = @{}
  try {
    foreach ($key in $privateSettings) {
      $saved[$key] = [Environment]::GetEnvironmentVariable($key)
      [Environment]::SetEnvironmentVariable($key, $null)
    }
    $process = Start-DevelopmentProcess 'provider' (Get-Command dotnet).Source @('bin/Debug/net10.0/FindYourFood.Provider.Api.dll', '--urls', 'http://localhost:5104') $providerApi
  } finally {
    foreach ($key in $privateSettings) { [Environment]::SetEnvironmentVariable($key, $saved[$key]) }
  }
  Wait-ForEndpoint 'http://localhost:5104/health/ready' $process
  'Provider is ready on localhost:5104.'
  $env:ConnectionStrings__Provider = $null
  $env:Gateway__EnableProvider = 'true'
  & "$PSScriptRoot/Start-AdministrationEnvironment.ps1" -SkipBuild
  & "$PSScriptRoot/Start-Frontend.ps1" -Provider
} catch {
  foreach ($name in $names) { Stop-DevelopmentProcess $name }
  throw
} finally {
  $env:ConnectionStrings__Provider = $previousConnection
  $env:Gateway__EnableProvider = $previousFlag
  Pop-Location
}
