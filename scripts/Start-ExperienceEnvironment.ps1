#requires -Version 7.0
. "$PSScriptRoot/Development.ps1"

$names = @('frontend', 'admin-frontend', 'gateway', 'identity', 'organization', 'experience')
foreach ($name in $names) {
  if (Get-DevelopmentProcess $name) { throw "$name is already running. Stop the current profile first." }
}

$previousConnection = $env:ConnectionStrings__Experience
$previousFlag = $env:Gateway__EnableExperience
Push-Location $projectRoot
try {
  Set-ExperienceDevelopmentConfiguration
  if (!(Test-Path -LiteralPath $env:Tokens__PublicKeyPath)) {
    throw 'Run Initialize-IdentityDatabase.ps1 to create the Identity public key first.'
  }
  Invoke-Checked 'dotnet' @('build', 'FindYourFood.slnx', '-m:1', '/nodeReuse:false')
  $privateSettings = @('ConnectionStrings__Identity', 'ConnectionStrings__Organization', 'Tokens__PrivateKeyPath')
  $saved = @{}
  try {
    foreach ($key in $privateSettings) {
      $saved[$key] = [Environment]::GetEnvironmentVariable($key)
      [Environment]::SetEnvironmentVariable($key, $null)
    }
    $process = Start-DevelopmentProcess 'experience' (Get-Command dotnet).Source @('bin/Debug/net10.0/FindYourFood.Experience.Api.dll', '--urls', 'http://localhost:5103') $experienceApi
  } finally {
    foreach ($key in $privateSettings) { [Environment]::SetEnvironmentVariable($key, $saved[$key]) }
  }
  Wait-ForEndpoint 'http://localhost:5103/health/ready' $process
  'Experience is ready on localhost:5103.'
  $env:ConnectionStrings__Experience = $null
  $env:Gateway__EnableExperience = 'true'
  & "$PSScriptRoot/Start-AdministrationEnvironment.ps1" -SkipBuild
  & "$PSScriptRoot/Start-Frontend.ps1"
} catch {
  foreach ($name in $names) { Stop-DevelopmentProcess $name }
  throw
} finally {
  $env:ConnectionStrings__Experience = $previousConnection
  $env:Gateway__EnableExperience = $previousFlag
  Pop-Location
}
