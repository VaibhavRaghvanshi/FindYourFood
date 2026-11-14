#requires -Version 7.0
. "$PSScriptRoot/Messaging.ps1"
if (Get-DevelopmentProcess 'rabbitmq') { throw 'The workspace broker is already running.' }
if (!(Test-Path -LiteralPath (Join-Path $rabbitSbin 'rabbitmq-server.bat'))) {
  throw 'Run Initialize-MessagingTools.ps1 first.'
}
$probe = [Net.Sockets.TcpClient]::new()
try {
  $connect = $probe.ConnectAsync('127.0.0.1', 5672)
  if ($connect.Wait(500) -and $probe.Connected) { throw 'Port 5672 is occupied. Stop that broker before starting this profile.' }
} catch [AggregateException] { } finally { $probe.Dispose() }
foreach ($directory in @($messagingState, (Join-Path $messagingState 'home'), (Join-Path $messagingState 'logs'))) {
  New-Item -ItemType Directory -Force $directory | Out-Null
}
if (!(Test-Path -LiteralPath $messagingPassword)) {
  [IO.File]::WriteAllText($messagingPassword, [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32)))
}
$arguments = Join-Path $messagingState 'erlang.args'
if (!(Test-Path -LiteralPath $arguments)) {
  $cookie = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
  [IO.File]::WriteAllText($arguments, "-setcookie $cookie")
}
$config = @(
  'listeners.tcp.1 = 127.0.0.1:5672',
  'default_user = findyourfood',
  "default_pass = $([IO.File]::ReadAllText($messagingPassword).Trim())",
  'default_vhost = findyourfood',
  'vm_memory_high_watermark.absolute = 256MiB',
  'disk_free_limit.absolute = 1GB',
  'max_message_size = 65536',
  'distribution.listener.port_range.min = 25672',
  'distribution.listener.port_range.max = 25672'
)
[IO.File]::WriteAllLines((Join-Path $messagingState 'rabbitmq.conf'), $config)
$keys = @('ERLANG_HOME', 'RABBITMQ_BASE', 'RABBITMQ_CONFIG_FILE', 'RABBITMQ_MNESIA_BASE', 'RABBITMQ_LOG_BASE',
  'RABBITMQ_NODENAME', 'RABBITMQ_SERVER_ADDITIONAL_ERL_ARGS', 'RABBITMQ_CTL_ERL_ARGS', 'ERL_EPMD_ADDRESS')
$saved = @{}
foreach ($key in $keys) { $saved[$key] = [Environment]::GetEnvironmentVariable($key) }
try {
  Set-RabbitDevelopmentEnvironment
  $process = Start-DevelopmentProcess 'rabbitmq' $env:ComSpec @('/d', '/c', 'rabbitmq-server.bat') $rabbitSbin
  $deadline = [DateTimeOffset]::UtcNow.AddSeconds(90)
  do {
    if ($process.HasExited) { throw 'RabbitMQ exited. See .local-state/rabbitmq logs.' }
    $client = [Net.Sockets.TcpClient]::new()
    try {
      $attempt = $client.ConnectAsync('127.0.0.1', 5672)
      if ($attempt.Wait(500) -and $client.Connected) {
        'RabbitMQ is listening on loopback port 5672.'
        return
      }
    } catch [AggregateException] { } finally { $client.Dispose() }
    Start-Sleep -Milliseconds 500
  } while ([DateTimeOffset]::UtcNow -lt $deadline)
  throw 'RabbitMQ did not become ready. See .local-state/rabbitmq logs.'
} catch {
  Stop-DevelopmentProcess 'rabbitmq'
  throw
} finally {
  foreach ($key in $keys) { [Environment]::SetEnvironmentVariable($key, $saved[$key]) }
}
