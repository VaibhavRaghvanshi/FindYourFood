#requires -Version 7.0
. "$PSScriptRoot/Development.ps1"

Push-Location $projectRoot
try {
  Set-OrganizationDevelopmentConfiguration
  Invoke-Checked 'dotnet' @('tool', 'restore')
  Invoke-Checked 'dotnet' @('ef', 'database', 'update', '--project', $organizationInfrastructure, '--startup-project', $organizationApi)
  Invoke-Checked 'dotnet' @('ef', 'migrations', 'has-pending-model-changes', '--no-build', '--project', $organizationInfrastructure, '--startup-project', $organizationApi)
} finally {
  Pop-Location
}
