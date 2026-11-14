#requires -Version 7.0
. "$PSScriptRoot/Messaging.ps1"
if (!(Get-DevelopmentProcess 'rabbitmq')) { return }
$keys = @('ERLANG_HOME', 'RABBITMQ_BASE', 'RABBITMQ_CONFIG_FILE', 'RABBITMQ_MNESIA_BASE', 'RABBITMQ_LOG_BASE',
  'RABBITMQ_NODENAME', 'RABBITMQ_SERVER_ADDITIONAL_ERL_ARGS', 'RABBITMQ_CTL_ERL_ARGS', 'ERL_EPMD_ADDRESS')
$saved = @{}
foreach ($key in $keys) { $saved[$key] = [Environment]::GetEnvironmentVariable($key) }
try {
  Set-RabbitDevelopmentEnvironment
  Push-Location $rabbitSbin
  try { Invoke-Checked './rabbitmqctl.bat' @('stop') } finally { Pop-Location }
  Stop-DevelopmentProcess 'rabbitmq'
} finally {
  foreach ($key in $keys) { [Environment]::SetEnvironmentVariable($key, $saved[$key]) }
}
