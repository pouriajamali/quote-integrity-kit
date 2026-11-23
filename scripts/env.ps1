# Dot-source from the repository root. Keep controlled caches/output on D:.
$env:DOTNET_CLI_HOME=Join-Path $PSScriptRoot '..\.local\dotnet'
$env:NUGET_PACKAGES=Join-Path $PSScriptRoot '..\.local\packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_NOLOGO='1'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE='true'
$env:TEMP=Join-Path $PSScriptRoot '..\.local\tmp'
$env:TMP=$env:TEMP
New-Item -ItemType Directory -Force -Path $env:TEMP | Out-Null
