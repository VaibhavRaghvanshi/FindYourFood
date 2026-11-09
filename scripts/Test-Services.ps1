#requires -Version 7.0
[CmdletBinding()]
param([switch]$EndToEnd)
. "$PSScriptRoot/Development.ps1"

if (!$env:FINDYOURFOOD_TEST_SQL) {
  throw 'Set FINDYOURFOOD_TEST_SQL to a local SQL Server connection with create database permission.'
}

Push-Location $projectRoot
try {
  Invoke-Checked 'dotnet' @('test', 'FindYourFood.slnx', '-m:1', '/nodeReuse:false')
  Push-Location $frontend
  try {
    Invoke-Checked 'npm.cmd' @('run', 'build')
    Invoke-Checked 'npm.cmd' @('test', '--', '--watch=false')
    if ($EndToEnd) {
      Invoke-Checked 'npm.cmd' @('run', 'e2e')
    }
  } finally {
    Pop-Location
  }
} finally {
  Pop-Location
}
