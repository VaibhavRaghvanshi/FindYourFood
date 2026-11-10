#requires -Version 7.0
[CmdletBinding()]
param([switch]$Admin, [switch]$Provider)
. "$PSScriptRoot/Development.ps1"
if ($Admin -and $Provider) { throw 'Select one frontend.' }
$name = if ($Admin) { 'admin-frontend' } elseif ($Provider) { 'provider-frontend' } else { 'frontend' }
$app = if ($Admin) { 'admin-portal' } elseif ($Provider) { 'provider-portal' } else { 'customer-web' }
$port = if ($Admin) { '4201' } elseif ($Provider) { '4202' } else { '4200' }
$url = "http://localhost:$port"
if (Get-DevelopmentProcess $name) { throw "$app is already running." }
if (!(Test-Path -LiteralPath (Join-Path $frontend 'node_modules/@angular/cli/bin/ng.js'))) {
  throw 'Run npm ci from frontend before starting Angular.'
}

$env:NG_CLI_ANALYTICS = 'false'
$privateSettings = @('ConnectionStrings__Identity', 'ConnectionStrings__Organization',
  'ConnectionStrings__Experience', 'ConnectionStrings__Provider', 'Tokens__PrivateKeyPath')
$saved = @{}
try {
  foreach ($key in $privateSettings) {
    $saved[$key] = [Environment]::GetEnvironmentVariable($key)
    [Environment]::SetEnvironmentVariable($key, $null)
  }
  $process = Start-DevelopmentProcess $name (Get-Command node.exe).Source @('node_modules/@angular/cli/bin/ng.js', 'serve', $app, '--host', 'localhost', '--port', $port) $frontend
} finally {
  foreach ($key in $privateSettings) { [Environment]::SetEnvironmentVariable($key, $saved[$key]) }
}
try {
  Wait-ForEndpoint $url $process -TimeoutSeconds 90
  "$app is listening on $url."
} catch {
  Stop-DevelopmentProcess $name
  throw
}
