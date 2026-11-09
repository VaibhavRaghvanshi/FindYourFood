#requires -Version 7.0
[CmdletBinding()]
param([switch]$Staff)
. "$PSScriptRoot/Development.ps1"

Push-Location $projectRoot
try {
  Set-IdentityDevelopmentConfiguration
  $action = if ($Staff) { '--enroll-staff' } else { '--enroll-owner' }
  Invoke-Checked 'dotnet' @('run', '--project', $identityApi, '--no-launch-profile', '--', $action)
} finally {
  Pop-Location
}
