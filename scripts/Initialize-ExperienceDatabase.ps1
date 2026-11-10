#requires -Version 7.0
. "$PSScriptRoot/Development.ps1"

Push-Location $projectRoot
try {
  Set-ExperienceDevelopmentConfiguration
  Invoke-Checked 'dotnet' @('tool', 'restore')
  Invoke-Checked 'dotnet' @('ef', 'database', 'update', '--project', $experienceInfrastructure, '--startup-project', $experienceApi)
  Invoke-Checked 'dotnet' @('ef', 'migrations', 'has-pending-model-changes', '--no-build', '--project', $experienceInfrastructure, '--startup-project', $experienceApi)
} finally {
  Pop-Location
}
