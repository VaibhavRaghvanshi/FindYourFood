$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$localTools = Join-Path $projectRoot '.local-tools'
$localState = Join-Path $projectRoot '.local-state'
$identityApi = Join-Path $projectRoot 'backend/services/Identity/src/FindYourFood.Identity.Api'
$identityInfrastructure = Join-Path $projectRoot 'backend/services/Identity/src/FindYourFood.Identity.Infrastructure'
$organizationApi = Join-Path $projectRoot 'backend/services/Organization/src/FindYourFood.Organization.Api'
$organizationInfrastructure = Join-Path $projectRoot 'backend/services/Organization/src/FindYourFood.Organization.Infrastructure'
$gateway = Join-Path $projectRoot 'backend/gateway/FindYourFood.ApiGateway'
$frontend = Join-Path $projectRoot 'frontend'

foreach ($directory in @('dotnet', 'node')) {
  $candidate = Join-Path $localTools $directory
  if (Test-Path -LiteralPath $candidate) {
    $env:Path = "$candidate;$env:Path"
  }
}

function Invoke-Checked {
  param([string]$Executable, [string[]]$Arguments)
  & $Executable @Arguments
  if ($LASTEXITCODE -ne 0) {
    throw "$Executable exited with code $LASTEXITCODE."
  }
}

function Set-IdentityDevelopmentConfiguration {
  if (!$env:ConnectionStrings__Identity) {
    $env:ConnectionStrings__Identity = 'Server=.\MSSQL22;Database=FindYourFood_IdentityDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
  }
  $env:ASPNETCORE_ENVIRONMENT = 'Development'
  $env:DOTNET_ENVIRONMENT = 'Development'
  $keys = Join-Path $localState 'keys'
  New-Item -ItemType Directory -Force $keys | Out-Null
  $env:Tokens__PublicKeyPath = Join-Path $keys 'identity-public.pem'
  $env:Tokens__PrivateKeyPath = Join-Path $keys 'identity-private.pem'
  $env:Identity__KeyRingPath = Join-Path $keys 'identity-protection'
  $hasPublicKey = Test-Path -LiteralPath $env:Tokens__PublicKeyPath
  $hasPrivateKey = Test-Path -LiteralPath $env:Tokens__PrivateKeyPath
  if ($hasPublicKey -ne $hasPrivateKey) { throw 'The local signing key pair is incomplete. Restore the missing key or move the remaining key out of .local-state/keys.' }
  if (!$hasPrivateKey) {
    $rsa = [Security.Cryptography.RSA]::Create(3072)
    try {
      [IO.File]::WriteAllText($env:Tokens__PrivateKeyPath, $rsa.ExportPkcs8PrivateKeyPem())
      [IO.File]::WriteAllText($env:Tokens__PublicKeyPath, $rsa.ExportSubjectPublicKeyInfoPem())
    } finally {
      $rsa.Dispose()
    }
  }
}

function Wait-ForEndpoint {
  param([string]$Url, [Diagnostics.Process]$Process, [int]$TimeoutSeconds = 60)
  $deadline = [DateTimeOffset]::UtcNow.AddSeconds($TimeoutSeconds)
  do {
    if ($Process -and $Process.HasExited) { throw "Process for $Url exited. See .local-state logs." }
    try {
      $response = Invoke-WebRequest -Uri $Url -TimeoutSec 2 -UseBasicParsing
      if ($response.StatusCode -eq 200) { return }
    } catch { Start-Sleep -Milliseconds 500 }
  } while ([DateTimeOffset]::UtcNow -lt $deadline)
  throw "$Url did not become ready. See .local-state logs."
}

function Set-OrganizationDevelopmentConfiguration {
  if (!$env:ConnectionStrings__Organization) {
    $env:ConnectionStrings__Organization = 'Server=.\MSSQL22;Database=FindYourFood_OrganizationDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
  }
  $env:ASPNETCORE_ENVIRONMENT = 'Development'
  $env:DOTNET_ENVIRONMENT = 'Development'
  $env:Tokens__PublicKeyPath = Join-Path $localState 'keys/identity-public.pem'
}

function Get-DevelopmentProcess {
  param([string]$Name)
  $path = Join-Path $localState "$Name.json"
  if (!(Test-Path -LiteralPath $path)) { return }
  $state = Get-Content -Raw -LiteralPath $path | ConvertFrom-Json
  $process = Get-Process -Id $state.Id -ErrorAction SilentlyContinue
  if ($process -and $process.StartTime.ToUniversalTime().Ticks -eq [long]$state.StartTicks) { return $process }
}

function Start-DevelopmentProcess {
  param([string]$Name, [string]$Executable, [string[]]$Arguments, [string]$WorkingDirectory)
  if (Get-DevelopmentProcess $Name) { throw "$Name is already running." }
  New-Item -ItemType Directory -Force $localState | Out-Null
  $options = @{
    FilePath = $Executable
    ArgumentList = $Arguments
    WorkingDirectory = $WorkingDirectory
    WindowStyle = 'Hidden'
    PassThru = $true
    RedirectStandardOutput = Join-Path $localState "$Name.stdout.log"
    RedirectStandardError = Join-Path $localState "$Name.stderr.log"
  }
  $process = Start-Process @options
  @{ Id = $process.Id; StartTicks = "$($process.StartTime.ToUniversalTime().Ticks)" } | ConvertTo-Json |
    Set-Content -LiteralPath (Join-Path $localState "$Name.json")
  return $process
}

function Stop-DevelopmentProcess {
  param([string]$Name)
  $process = Get-DevelopmentProcess $Name
  if ($process) { Stop-Process -Id $process.Id -ErrorAction SilentlyContinue }
  $path = Join-Path $localState "$Name.json"
  if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
}
