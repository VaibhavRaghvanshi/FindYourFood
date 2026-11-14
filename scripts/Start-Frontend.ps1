#requires -Version 7.0
[CmdletBinding()]
param([switch]$Admin, [switch]$Provider)
. "$PSScriptRoot/Development.ps1"
if ($Admin -and $Provider) { throw 'Select one frontend.' }
$name = if ($Admin) { 'admin-frontend' } elseif ($Provider) { 'provider-frontend' } else { 'frontend' }
$app = if ($Admin) { 'admin-portal' } elseif ($Provider) { 'provider-portal' } else { 'customer-web' }
$port = if ($Admin) { '4201' } elseif ($Provider) { '4202' } else { '4200' }
$offset = if ($env:FINDYOURFOOD_E2E_PORT_OFFSET) { [int]$env:FINDYOURFOOD_E2E_PORT_OFFSET } else { 0 }
$port = [string]([int]$port + $offset)
$url = "http://localhost:$port"
if (Get-DevelopmentProcess $name) { throw "$app is already running." }
$portProbe = [Net.Sockets.TcpClient]::new()
try {
  $attempt = $portProbe.ConnectAsync('localhost', [int]$port)
  if ($attempt.Wait(500) -and $portProbe.Connected) { throw "Port $port is occupied. Stop its app or use a different test port offset." }
} catch [AggregateException] { } finally { $portProbe.Dispose() }
if (!(Test-Path -LiteralPath (Join-Path $frontend 'node_modules/@angular/cli/bin/ng.js'))) {
  throw 'Run npm ci from frontend before starting Angular.'
}

$env:NG_CLI_ANALYTICS = 'false'
$privateSettings = @('ConnectionStrings__Identity', 'ConnectionStrings__Organization',
  'ConnectionStrings__Experience', 'ConnectionStrings__Provider', 'Tokens__PrivateKeyPath',
  'ConnectionStrings__Catalog', 'ConnectionStrings__Customer', 'ConnectionStrings__Location',
  'ConnectionStrings__Discovery', 'ConnectionStrings__Cart', 'ConnectionStrings__Ordering',
  'ConnectionStrings__Payment', 'Messaging__PasswordPath', 'Payment__WebhookKeyPath')
$saved = @{}
try {
  foreach ($key in $privateSettings) {
    $saved[$key] = [Environment]::GetEnvironmentVariable($key)
    [Environment]::SetEnvironmentVariable($key, $null)
  }
  if ($env:FINDYOURFOOD_STATIC_FRONTEND -eq 'true') {
    $assets = Join-Path $frontend "dist/$app/browser"
    if (!(Test-Path -LiteralPath (Join-Path $assets 'index.html'))) { throw 'Build the Angular apps before starting the static test profile.' }
    $process = Start-DevelopmentProcess $name (Get-Command node.exe).Source @((Join-Path $PSScriptRoot 'Serve-Frontend.mjs'), $assets, $port) $frontend
  } else {
    $process = Start-DevelopmentProcess $name (Get-Command node.exe).Source @('node_modules/@angular/cli/bin/ng.js', 'serve', $app, '--host', 'localhost', '--port', $port) $frontend
  }
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
