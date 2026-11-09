#requires -Version 7.0
[CmdletBinding()]
param([switch]$SkipBuild)
. "$PSScriptRoot/Development.ps1"

if ((Get-DevelopmentProcess 'identity') -or (Get-DevelopmentProcess 'gateway')) {
  throw 'An Identity or gateway process is already running.'
}

Push-Location $projectRoot
$started = [Collections.Generic.List[string]]::new()
try {
  Set-IdentityDevelopmentConfiguration
  if (!$SkipBuild) {
    Invoke-Checked 'dotnet' @('build', 'FindYourFood.slnx', '-m:1', '/nodeReuse:false')
  }
  $identity = Start-DevelopmentProcess 'identity' (Get-Command dotnet).Source @('bin/Debug/net10.0/FindYourFood.Identity.Api.dll', '--urls', 'http://localhost:5101') $identityApi
  $started.Add('identity')
  Wait-ForEndpoint 'http://localhost:5101/health/ready' $identity
  $privateKey = $env:Tokens__PrivateKeyPath
  $connection = $env:ConnectionStrings__Identity
  try {
    Remove-Item Env:Tokens__PrivateKeyPath
    Remove-Item Env:ConnectionStrings__Identity
    $proxy = Start-DevelopmentProcess 'gateway' (Get-Command dotnet).Source @('bin/Debug/net10.0/FindYourFood.ApiGateway.dll', '--urls', 'http://localhost:5100') $gateway
    $started.Add('gateway')
  } finally {
    $env:Tokens__PrivateKeyPath = $privateKey
    $env:ConnectionStrings__Identity = $connection
  }
  Wait-ForEndpoint 'http://localhost:5100/health/live' $proxy
  'Identity is ready on localhost:5101. Gateway is listening on localhost:5100.'
} catch {
  foreach ($name in $started) { Stop-DevelopmentProcess $name }
  throw
} finally {
  Pop-Location
}
