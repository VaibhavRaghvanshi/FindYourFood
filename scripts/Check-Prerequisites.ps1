#requires -Version 7.0
. "$PSScriptRoot/Development.ps1"
Push-Location $projectRoot
try {
  Invoke-Checked 'git' @('--version')
  $sdk = & dotnet --version
  if ($LASTEXITCODE -ne 0 -or $sdk -notmatch '^10\.0\.\d+$') { throw 'Install the stable .NET SDK specified in global.json.' }
  ".NET SDK $sdk"
  $node = & node.exe --version
  if ($LASTEXITCODE -ne 0 -or $node -notmatch '^v(\d+)\.(\d+)\.(\d+)$') { throw 'Install a stable Angular-compatible Node.js version.' }
  $version = [version]$node.Substring(1)
  if (!(($version.Major -eq 22 -and $version -ge [version]'22.22.3') -or
        ($version.Major -eq 24 -and $version -ge [version]'24.15.0') -or $version.Major -ge 26)) {
    throw "Node $version is incompatible with Angular 22."
  }
  "Node.js $node"
  Invoke-Checked 'npm.cmd' @('--version')
  $sqlServices = Get-Service 'MSSQL*' -ErrorAction SilentlyContinue | Where-Object Status -eq Running
  $sqlServices | Select-Object Name, Status
  'Identity validates the configured SQL connection during migration and readiness checks.'
} finally {
  Pop-Location
}
