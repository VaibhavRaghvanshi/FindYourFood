#requires -Version 7.0
[CmdletBinding()]
param([switch]$Experience, [switch]$Provider, [switch]$Marketplace)
. "$PSScriptRoot/Development.ps1"

if ($Marketplace) { $Provider = $true }
if ($Experience -and $Provider) { throw 'Select one browser profile.' }
$processNames = @('identity', 'gateway', 'organization', 'admin-frontend')
if ($Experience) { $processNames += @('experience', 'frontend') }
if ($Provider) { $processNames += @('provider', 'provider-frontend') }
if ($Marketplace) { $processNames += @('catalog', 'customer', 'location', 'discovery', 'cart', 'frontend') }
foreach ($name in $processNames) {
  if (Get-DevelopmentProcess $name) { throw "$name is already running. Stop the current profile first." }
}
if (!$env:FINDYOURFOOD_TEST_SQL) {
  throw 'Set FINDYOURFOOD_TEST_SQL to a local SQL Server connection with create database permission.'
}

Push-Location $projectRoot
$original = @{
  Identity = $env:ConnectionStrings__Identity
  Organization = $env:ConnectionStrings__Organization
  Experience = $env:ConnectionStrings__Experience
  ExperienceTests = $env:FINDYOURFOOD_E2E_EXPERIENCE
  Provider = $env:ConnectionStrings__Provider
  ProviderTests = $env:FINDYOURFOOD_E2E_PROVIDER
  Accounts = $env:FINDYOURFOOD_E2E_ACCOUNTS
}
$seed = 'backend/integration-tests/FindYourFood.Administration.Seed'
$run = [guid]::NewGuid().ToString('N')
$readyToClean = $false
$profileStarted = $false
$seedOptions = @()
$marketplaceOriginal = @{}
foreach ($service in @('Catalog', 'Customer', 'Location', 'Discovery', 'Cart')) {
  $key = "ConnectionStrings__$service"
  $marketplaceOriginal[$key] = [Environment]::GetEnvironmentVariable($key)
}
$marketplaceFlag = $env:FINDYOURFOOD_E2E_MARKETPLACE
try {
  $identity = [Data.Common.DbConnectionStringBuilder]::new()
  $identity.set_ConnectionString($env:FINDYOURFOOD_TEST_SQL)
  [void]$identity.Remove('Database')
  $identity['Initial Catalog'] = "FindYourFood_IdentityTest_$run"
  $env:ConnectionStrings__Identity = $identity.get_ConnectionString()
  $identity['Initial Catalog'] = "FindYourFood_OrganizationTest_$run"
  $env:ConnectionStrings__Organization = $identity.get_ConnectionString()
  $env:FINDYOURFOOD_E2E_EXPERIENCE = if ($Experience) { 'true' } else { $null }
  $env:FINDYOURFOOD_E2E_PROVIDER = if ($Provider) { 'true' } else { $null }
  if ($Experience) {
    $identity['Initial Catalog'] = "FindYourFood_ExperienceTest_$run"
    $env:ConnectionStrings__Experience = $identity.get_ConnectionString()
    $seedOptions += '--experience'
  }
  if ($Provider) {
    $identity['Initial Catalog'] = "FindYourFood_ProviderTest_$run"
    $env:ConnectionStrings__Provider = $identity.get_ConnectionString()
    $seedOptions += '--provider'
  }
  $env:FINDYOURFOOD_E2E_MARKETPLACE = if ($Marketplace) { 'true' } else { $null }
  if ($Marketplace) {
    foreach ($service in @('Catalog', 'Customer', 'Location', 'Discovery', 'Cart')) {
      $identity['Initial Catalog'] = "FindYourFood_${service}Test_$run"
      [Environment]::SetEnvironmentVariable("ConnectionStrings__$service", $identity.get_ConnectionString())
    }
    $seedOptions += '--marketplace'
  }
  Set-IdentityDevelopmentConfiguration
  $env:FINDYOURFOOD_E2E_ACCOUNTS = Join-Path $localState "admin-accounts-$run.json"
  $readyToClean = $true
  Invoke-Checked 'dotnet' (@('run', '--project', $seed, '--') + $seedOptions)
  if ($Experience) {
    & "$PSScriptRoot/Start-ExperienceEnvironment.ps1"
  } elseif ($Marketplace) {
    & "$PSScriptRoot/Start-MarketplaceEnvironment.ps1" -Services Discovery,Customer,Cart
  } elseif ($Provider) {
    & "$PSScriptRoot/Start-ProviderEnvironment.ps1"
  } else {
    & "$PSScriptRoot/Start-AdministrationEnvironment.ps1"
  }
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
      foreach ($name in $processNames) { Stop-DevelopmentProcess $name }
    }
    if ($readyToClean) {
      Invoke-Checked 'dotnet' (@('run', '--project', $seed, '--no-build', '--', '--cleanup') + $seedOptions)
    }
  } finally {
    $env:ConnectionStrings__Identity = $original.Identity
    $env:ConnectionStrings__Organization = $original.Organization
    $env:ConnectionStrings__Experience = $original.Experience
    $env:FINDYOURFOOD_E2E_EXPERIENCE = $original.ExperienceTests
    $env:ConnectionStrings__Provider = $original.Provider
    $env:FINDYOURFOOD_E2E_PROVIDER = $original.ProviderTests
    $env:FINDYOURFOOD_E2E_ACCOUNTS = $original.Accounts
    $env:FINDYOURFOOD_E2E_MARKETPLACE = $marketplaceFlag
    foreach ($key in $marketplaceOriginal.Keys) {
      [Environment]::SetEnvironmentVariable($key, $marketplaceOriginal[$key])
    }
    Pop-Location
  }
}
