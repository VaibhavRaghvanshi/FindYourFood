#requires -Version 7.0
. "$PSScriptRoot/Development.ps1"

Push-Location $projectRoot
try {
  Set-IdentityDevelopmentConfiguration
  Invoke-Checked 'dotnet' @('tool', 'restore')
  Invoke-Checked 'dotnet' @('ef', 'database', 'update', '--project', $identityInfrastructure, '--startup-project', $identityApi)
  Invoke-Checked 'dotnet' @('ef', 'migrations', 'has-pending-model-changes', '--no-build', '--project', $identityInfrastructure, '--startup-project', $identityApi)
} finally {
  Pop-Location
}
