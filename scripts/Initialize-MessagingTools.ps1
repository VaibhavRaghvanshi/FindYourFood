#requires -Version 7.0
. "$PSScriptRoot/Development.ps1"

$downloads = Join-Path $localTools 'downloads'
New-Item -ItemType Directory -Force $downloads | Out-Null

function Get-VerifiedArtifact {
  param([string]$ArtifactUri, [string]$Sha256)
  $path = Join-Path $downloads ([IO.Path]::GetFileName(([uri]$ArtifactUri).LocalPath))
  if (!(Test-Path -LiteralPath $path)) {
    Invoke-WebRequest -Uri $ArtifactUri -OutFile "$path.download"
    if ((Get-FileHash -LiteralPath "$path.download" -Algorithm SHA256).Hash -ne $Sha256) {
      throw "Artifact hash did not match for $([IO.Path]::GetFileName($path))."
    }
    Move-Item -LiteralPath "$path.download" -Destination $path
  }
  if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $Sha256) {
    throw "Artifact hash did not match for $([IO.Path]::GetFileName($path))."
  }
  return $path
}

# Pinned publisher release assets. SHA-256 digests are recorded in their GitHub releases.
$bootstrap = Get-VerifiedArtifact 'https://github.com/ip7z/7zip/releases/download/26.06/7zr.exe' 'e80db2dad715a6c5adabe292d31ed54e11f4e9b99ca349827b50573b175891bc'
$archive = Get-VerifiedArtifact 'https://github.com/ip7z/7zip/releases/download/26.06/7z2606-x64.exe' '9b697f65ecc2d9b0152f5ee96d72dde522a7827690349d9307935dacf40f42ec'
$sevenZip = Join-Path $localTools '7zip'
if (!(Test-Path -LiteralPath (Join-Path $sevenZip '7z.exe'))) {
  Invoke-Checked $bootstrap @('x', $archive, "-o$sevenZip", '-y')
}
$extractor = Join-Path $sevenZip '7z.exe'

$erlangArchive = Get-VerifiedArtifact 'https://github.com/erlang/otp/releases/download/OTP-29.1.2/otp_win64_29.1.2.exe' '38297ee6fa8cafbc016cb187ebda30f7bdae85618415d9dbbbe413e9b9e27a4e'
$erlang = Join-Path $localTools 'erlang'
if (!(Test-Path -LiteralPath (Join-Path $erlang 'bin/erl.exe'))) {
  Invoke-Checked $extractor @('x', $erlangArchive, "-o$erlang", '-y')
  Push-Location $erlang
  try { Invoke-Checked './Install.exe' @('-sasl') } finally { Pop-Location }
}

$rabbitArchive = Get-VerifiedArtifact 'https://github.com/rabbitmq/rabbitmq-server/releases/download/v4.4.0/rabbitmq-server-windows-4.4.0.zip' 'c077a084890e57e1498e479913d4d5bda77e88647e153b433e97f72a31431542'
$rabbitRoot = Join-Path $localTools 'rabbitmq'
if (!(Test-Path -LiteralPath (Join-Path $rabbitRoot 'rabbitmq_server-4.4.0/sbin/rabbitmq-server.bat'))) {
  Expand-Archive -LiteralPath $rabbitArchive -DestinationPath $rabbitRoot -Force
}
$previousErlang = $env:ERLANG_HOME
try {
  $env:ERLANG_HOME = $erlang
  Invoke-Checked (Join-Path $erlang 'bin/erl.exe') @('-noshell', '-eval', 'io:format("Erlang ~s~n", [erlang:system_info(otp_release)]), halt().')
} finally { $env:ERLANG_HOME = $previousErlang }
'Native messaging tools are ready under .local-tools.'
