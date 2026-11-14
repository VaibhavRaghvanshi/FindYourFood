. "$PSScriptRoot/Development.ps1"
$messagingState = Join-Path $localState 'messaging'
$messagingPassword = Join-Path $messagingState 'password.txt'
$rabbitSbin = Join-Path $localTools 'rabbitmq/rabbitmq_server-4.4.0/sbin'

function Set-MessagingDevelopmentConfiguration {
  if (!(Test-Path -LiteralPath $messagingPassword)) { throw 'Run Start-MessagingBroker.ps1 first.' }
  $env:Messaging__Enabled = 'true'
  $env:Messaging__Host = 'localhost'
  $env:Messaging__Port = '5672'
  $env:Messaging__User = 'findyourfood'
  $env:Messaging__VirtualHost = 'findyourfood'
  $env:Messaging__PasswordPath = $messagingPassword
}

function Set-RabbitDevelopmentEnvironment {
  $env:ERLANG_HOME = Join-Path $localTools 'erlang'
  $env:RABBITMQ_BASE = $messagingState
  $env:RABBITMQ_CONFIG_FILE = Join-Path $messagingState 'rabbitmq'
  $env:RABBITMQ_MNESIA_BASE = Join-Path $messagingState 'data'
  $env:RABBITMQ_LOG_BASE = Join-Path $messagingState 'logs'
  $env:RABBITMQ_NODENAME = 'findyourfood@localhost'
  $arguments = Join-Path $messagingState 'erlang.args'
  $env:RABBITMQ_SERVER_ADDITIONAL_ERL_ARGS = "+S 2:2 +A 4 -args_file `"$arguments`" -kernel inet_dist_use_interface {127,0,0,1}"
  $env:RABBITMQ_CTL_ERL_ARGS = "+S 1:1 +A 2 -args_file `"$arguments`""
  $env:ERL_EPMD_ADDRESS = '127.0.0.1'
}
