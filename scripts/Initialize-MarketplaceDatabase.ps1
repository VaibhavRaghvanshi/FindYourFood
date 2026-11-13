#requires -Version 7.0
[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('Catalog', 'Customer', 'Location')][string]$Service)
. "$PSScriptRoot/Development.ps1"

$root = "backend/services/FindYourFood.$Service.Api"
$api = "$root/FindYourFood.$Service.Api"
$infrastructure = "$root/FindYourFood.$Service.Infrastructure"
$key = "ConnectionStrings__$Service"
$previous = [Environment]::GetEnvironmentVariable($key)
Push-Location $projectRoot
try {
  if (!$previous) {
    [Environment]::SetEnvironmentVariable($key, "Server=.\MSSQL22;Database=FindYourFood_${Service}Db;Integrated Security=True;Encrypt=True;TrustServerCertificate=True")
  }
  Invoke-Checked 'dotnet' @('tool', 'restore')
  Invoke-Checked 'dotnet' @('ef', 'database', 'update', '--project', $infrastructure, '--startup-project', $api)
  Invoke-Checked 'dotnet' @('ef', 'migrations', 'has-pending-model-changes', '--no-build', '--project', $infrastructure, '--startup-project', $api)
} finally {
  [Environment]::SetEnvironmentVariable($key, $previous)
  Pop-Location
}
