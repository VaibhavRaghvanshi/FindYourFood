#requires -Version 7.0
. "$PSScriptRoot/Development.ps1"
if (Get-DevelopmentProcess 'frontend') { throw 'The customer frontend is already running.' }
if (!(Test-Path -LiteralPath (Join-Path $frontend 'node_modules/@angular/cli/bin/ng.js'))) {
  throw 'Run npm ci from frontend before starting Angular.'
}

$env:NG_CLI_ANALYTICS = 'false'
$process = Start-DevelopmentProcess 'frontend' (Get-Command node.exe).Source @('node_modules/@angular/cli/bin/ng.js', 'serve', 'customer-web', '--host', 'localhost', '--port', '4200') $frontend
try {
  Wait-ForEndpoint 'http://localhost:4200' $process -TimeoutSeconds 90
  'Customer application is listening on http://localhost:4200.'
} catch {
  Stop-DevelopmentProcess 'frontend'
  throw
}
