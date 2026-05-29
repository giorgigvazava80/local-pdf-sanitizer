param(
    [ValidateSet("win-x64", "win-arm64")]
    [string] $Runtime = "win-x64",

    [switch] $NoRestore
)

$ErrorActionPreference = "Stop"

$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
$Project = Join-Path $Root "native-host\PdfSanitizerHost\PdfSanitizerHost.csproj"
$Output = Join-Path $Root "native-host\publish"
$RepoDotnet = Join-Path $Root ".dotnet\dotnet.exe"

$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:DOTNET_NOLOGO = "1"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"

if (Test-Path $RepoDotnet) {
    $Dotnet = $RepoDotnet
} elseif (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $Dotnet = "dotnet"
} else {
    throw ".NET SDK was not found. Install .NET SDK 8 or newer, then rerun this script."
}

$PublishArgs = @(
    "publish",
    $Project,
    "--configuration",
    "Release",
    "--runtime",
    $Runtime,
    "--self-contained",
    "true",
    "-p:PublishSingleFile=true",
    "-p:DebugType=None",
    "-p:DebugSymbols=false",
    "--output",
    $Output
)

if ($NoRestore) {
    $PublishArgs += "--no-restore"
}

& $Dotnet @PublishArgs

Write-Host "Native host published to $Output"
