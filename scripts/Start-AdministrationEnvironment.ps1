#requires -Version 7.0
[CmdletBinding()]
param([switch]$SkipBuild)
. "$PSScriptRoot/Development.ps1"

foreach ($name in @('identity', 'gateway', 'organization', 'admin-frontend')) {
  if (Get-DevelopmentProcess $name) { throw "$name is already running. Stop the current profile first." }
}
$previous = $env:Gateway__EnableOrganization
Push-Location $projectRoot
try {
  $env:Gateway__EnableOrganization = 'true'
  if (!$SkipBuild) {
    Invoke-Checked 'dotnet' @('build', 'FindYourFood.slnx', '-m:1', '/nodeReuse:false')
  }
  & "$PSScriptRoot/Start-OrganizationEnvironment.ps1" -SkipBuild
  $organizationStarted = $true
  & "$PSScriptRoot/Start-IdentityEnvironment.ps1" -SkipBuild
  $identityStarted = $true
  & "$PSScriptRoot/Start-Frontend.ps1" -Admin
} catch {
  if ($identityStarted) {
    Stop-DevelopmentProcess 'gateway'
    Stop-DevelopmentProcess 'identity'
  }
  if ($organizationStarted) { Stop-DevelopmentProcess 'organization' }
  throw
} finally {
  $env:Gateway__EnableOrganization = $previous
  Pop-Location
}
