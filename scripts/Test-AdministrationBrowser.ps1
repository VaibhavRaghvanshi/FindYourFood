#requires -Version 7.0
[CmdletBinding()]
param([switch]$Experience, [switch]$Provider, [switch]$Marketplace, [switch]$Payments, [switch]$SkipBuild)
. "$PSScriptRoot/Development.ps1"

if ($Payments) { $Marketplace = $true }
if ($Marketplace) { $Provider = $true }
if ($Experience -and $Provider) { throw 'Select one browser profile.' }
$processNames = @('identity', 'gateway', 'organization', 'admin-frontend')
if ($Experience) { $processNames += @('experience', 'frontend') }
if ($Provider) { $processNames += @('provider', 'provider-frontend') }
if ($Marketplace) { $processNames += @('catalog', 'customer', 'location', 'discovery', 'cart', 'ordering', 'frontend') }
if ($Payments) { $processNames += 'payment' }
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
foreach ($service in @('Catalog', 'Customer', 'Location', 'Discovery', 'Cart', 'Ordering', 'Payment')) {
  $key = "ConnectionStrings__$service"
  $marketplaceOriginal[$key] = [Environment]::GetEnvironmentVariable($key)
}
$marketplaceFlag = $env:FINDYOURFOOD_E2E_MARKETPLACE
$paymentsFlag = $env:FINDYOURFOOD_E2E_PAYMENTS
$messagingPrefix = $env:Messaging__Prefix
$brokerWasRunning = Get-DevelopmentProcess 'rabbitmq'
$staticFrontend = $env:FINDYOURFOOD_STATIC_FRONTEND
$portOffset = $env:FINDYOURFOOD_E2E_PORT_OFFSET
try {
  if (!$env:FINDYOURFOOD_E2E_PORT_OFFSET) { $env:FINDYOURFOOD_E2E_PORT_OFFSET = '100' }
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
    foreach ($service in @('Catalog', 'Customer', 'Location', 'Discovery', 'Cart', 'Ordering')) {
      $identity['Initial Catalog'] = "FindYourFood_${service}Test_$run"
      [Environment]::SetEnvironmentVariable("ConnectionStrings__$service", $identity.get_ConnectionString())
    }
    $seedOptions += '--marketplace'
  }
  $env:FINDYOURFOOD_E2E_PAYMENTS = if ($Payments) { 'true' } else { $null }
  if ($Payments) {
    $identity['Initial Catalog'] = "FindYourFood_PaymentTest_$run"
    $env:ConnectionStrings__Payment = $identity.get_ConnectionString()
    $env:Messaging__Prefix = "findyourfood.test.$run"
    $seedOptions += '--payments'
  }
  Set-IdentityDevelopmentConfiguration
  $env:FINDYOURFOOD_E2E_ACCOUNTS = Join-Path $localState "admin-accounts-$run.json"
  $readyToClean = $true
  $seedRun = @('run', '--project', $seed)
  if ($SkipBuild) { $seedRun += '--no-build' }
  Invoke-Checked 'dotnet' ($seedRun + @('--') + $seedOptions)
  if ($Payments) {
    if (!$SkipBuild) {
      Push-Location $frontend
      try { Invoke-Checked 'npm.cmd' @('run', 'build') } finally { Pop-Location }
    }
    $env:FINDYOURFOOD_STATIC_FRONTEND = 'true'
  }
  if ($Experience) {
    & "$PSScriptRoot/Start-ExperienceEnvironment.ps1"
  } elseif ($Marketplace) {
    $marketplaceServices = @('Discovery', 'Customer', 'Ordering')
    if ($Payments) { $marketplaceServices += 'Payment' }
    & "$PSScriptRoot/Start-MarketplaceEnvironment.ps1" -Services $marketplaceServices -SkipBuild:$SkipBuild
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
  if ($Payments) {
    Invoke-Checked 'dotnet' (@('run', '--project', $seed, '--no-build', '--', '--verify-payments') + $seedOptions)
  }
  @{ Run = $run; CompletedAt = [DateTimeOffset]::UtcNow.ToString('o');
     Profile = $(if ($Payments) { 'payments' } elseif ($Marketplace) { 'marketplace' } elseif ($Experience) { 'experience' } elseif ($Provider) { 'provider' } else { 'staff' });
     BrowserPassed = $true; BrokerVerified = [bool]$Payments } | ConvertTo-Json |
    Set-Content -LiteralPath (Join-Path $localState "browser-result-$run.json")
} catch {
  if ($Payments -and $profileStarted) {
    & dotnet run --project $seed --no-build -- --diagnose-payments @seedOptions
  }
  throw
} finally {
  try {
    if ($profileStarted) {
      foreach ($name in $processNames) { Stop-DevelopmentProcess $name }
    }
    if ($readyToClean) {
      if ($Payments -and (Get-DevelopmentProcess 'rabbitmq')) {
        . "$PSScriptRoot/Messaging.ps1"
        $savedPasswordPath = $env:Messaging__PasswordPath
        try {
          $env:Messaging__PasswordPath = $messagingPassword
          Invoke-Checked 'dotnet' (@('run', '--project', $seed, '--no-build', '--', '--cleanup') + $seedOptions)
        } finally {
          $env:Messaging__PasswordPath = $savedPasswordPath
        }
      } else {
        Invoke-Checked 'dotnet' (@('run', '--project', $seed, '--no-build', '--', '--cleanup') + $seedOptions)
      }
    }
    if ($Payments -and !$brokerWasRunning) { & "$PSScriptRoot/Stop-MessagingBroker.ps1" }
  } finally {
    $env:ConnectionStrings__Identity = $original.Identity
    $env:ConnectionStrings__Organization = $original.Organization
    $env:ConnectionStrings__Experience = $original.Experience
    $env:FINDYOURFOOD_E2E_EXPERIENCE = $original.ExperienceTests
    $env:ConnectionStrings__Provider = $original.Provider
    $env:FINDYOURFOOD_E2E_PROVIDER = $original.ProviderTests
    $env:FINDYOURFOOD_E2E_ACCOUNTS = $original.Accounts
    $env:FINDYOURFOOD_E2E_MARKETPLACE = $marketplaceFlag
    $env:FINDYOURFOOD_E2E_PAYMENTS = $paymentsFlag
    $env:Messaging__Prefix = $messagingPrefix
    $env:FINDYOURFOOD_STATIC_FRONTEND = $staticFrontend
    $env:FINDYOURFOOD_E2E_PORT_OFFSET = $portOffset
    foreach ($key in $marketplaceOriginal.Keys) {
      [Environment]::SetEnvironmentVariable($key, $marketplaceOriginal[$key])
    }
    Pop-Location
  }
}
