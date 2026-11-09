#requires -Version 7.0
. "$PSScriptRoot/Development.ps1"

foreach ($name in @('identity', 'gateway', 'organization', 'admin-frontend')) {
  if (Get-DevelopmentProcess $name) { throw "$name is already running. Stop the current profile first." }
}
if (!$env:FINDYOURFOOD_TEST_SQL) {
  throw 'Set FINDYOURFOOD_TEST_SQL to a local SQL Server connection with create database permission.'
}

Push-Location $projectRoot
$original = @{
  Identity = $env:ConnectionStrings__Identity
  Organization = $env:ConnectionStrings__Organization
  Accounts = $env:FINDYOURFOOD_E2E_ACCOUNTS
}
$seed = 'backend/integration-tests/FindYourFood.Administration.Seed'
$run = [guid]::NewGuid().ToString('N')
$readyToClean = $false
$profileStarted = $false
try {
  $identity = [Data.Common.DbConnectionStringBuilder]::new()
  $identity.set_ConnectionString($env:FINDYOURFOOD_TEST_SQL)
  [void]$identity.Remove('Database')
  $identity['Initial Catalog'] = "FindYourFood_IdentityTest_$run"
  $env:ConnectionStrings__Identity = $identity.get_ConnectionString()
  $identity['Initial Catalog'] = "FindYourFood_OrganizationTest_$run"
  $env:ConnectionStrings__Organization = $identity.get_ConnectionString()
  Set-IdentityDevelopmentConfiguration
  $env:FINDYOURFOOD_E2E_ACCOUNTS = Join-Path $localState "admin-accounts-$run.json"
  $readyToClean = $true
  Invoke-Checked 'dotnet' @('run', '--project', $seed)
  & "$PSScriptRoot/Start-AdministrationEnvironment.ps1"
  $profileStarted = $true
  Push-Location $frontend
  try {
    Invoke-Checked 'npm.cmd' @('run', 'e2e:admin')
  } finally {
    Pop-Location
  }
} finally {
  try {
    if ($profileStarted) {
      foreach ($name in @('admin-frontend', 'gateway', 'organization', 'identity')) { Stop-DevelopmentProcess $name }
    }
    if ($readyToClean) {
      Invoke-Checked 'dotnet' @('run', '--project', $seed, '--no-build', '--', '--cleanup')
    }
  } finally {
    $env:ConnectionStrings__Identity = $original.Identity
    $env:ConnectionStrings__Organization = $original.Organization
    $env:FINDYOURFOOD_E2E_ACCOUNTS = $original.Accounts
    Pop-Location
  }
}
