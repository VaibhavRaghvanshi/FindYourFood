#requires -Version 7.0
[CmdletBinding()]
param([switch]$SkipBuild)
. "$PSScriptRoot/Development.ps1"

if (Get-DevelopmentProcess 'organization') { throw 'Organization is already running.' }
Push-Location $projectRoot
try {
  Set-OrganizationDevelopmentConfiguration
  if (!(Test-Path -LiteralPath $env:Tokens__PublicKeyPath)) {
    throw 'Run Initialize-IdentityDatabase.ps1 to create the Identity public key first.'
  }
  if (!$SkipBuild) {
    Invoke-Checked 'dotnet' @('build', $organizationApi, '-m:1', '/nodeReuse:false')
  }
  $privateKey = $env:Tokens__PrivateKeyPath
  $identityConnection = $env:ConnectionStrings__Identity
  try {
    $env:Tokens__PrivateKeyPath = $null
    $env:ConnectionStrings__Identity = $null
    $process = Start-DevelopmentProcess 'organization' (Get-Command dotnet).Source @('bin/Debug/net10.0/FindYourFood.Organization.Api.dll', '--urls', 'http://localhost:5102') $organizationApi
  } finally {
    $env:Tokens__PrivateKeyPath = $privateKey
    $env:ConnectionStrings__Identity = $identityConnection
  }
  Wait-ForEndpoint 'http://localhost:5102/health/ready' $process
  'Organization is ready on localhost:5102.'
} catch {
  if ($process) { Stop-DevelopmentProcess 'organization' }
  throw
} finally {
  Pop-Location
}
