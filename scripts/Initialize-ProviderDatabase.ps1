#requires -Version 7.0
. "$PSScriptRoot/Development.ps1"

Push-Location $projectRoot
try {
  Set-ProviderDevelopmentConfiguration
  Invoke-Checked 'dotnet' @('tool', 'restore')
  Invoke-Checked 'dotnet' @('ef', 'database', 'update', '--project', $providerInfrastructure, '--startup-project', $providerApi)
  Invoke-Checked 'dotnet' @('ef', 'migrations', 'has-pending-model-changes', '--no-build', '--project', $providerInfrastructure, '--startup-project', $providerApi)
} finally {
  Pop-Location
}
