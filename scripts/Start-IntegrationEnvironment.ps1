#requires -Version 7.0
. "$PSScriptRoot/Development.ps1"
if (Get-DevelopmentProcess 'frontend') { throw 'The frontend is already running.' }

& "$PSScriptRoot/Start-IdentityEnvironment.ps1"
try {
  & "$PSScriptRoot/Start-Frontend.ps1"
} catch {
  Stop-DevelopmentProcess 'gateway'
  Stop-DevelopmentProcess 'identity'
  throw
}
