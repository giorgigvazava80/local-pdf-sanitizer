param(
    [Parameter(Mandatory = $true)]
    [string[]] $ExtensionId,

    [ValidateSet("Chrome", "Edge", "Both")]
    [string] $Browser = "Both",

    [ValidateSet("win-x64", "win-arm64")]
    [string] $Runtime = "win-x64",

    [ValidateSet("User", "Machine")]
    [string] $Scope = "User",

    [switch] $SkipBuild
)

$ErrorActionPreference = "Stop"

$HostName = "com.pdf_checker.sanitizer"
$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
$HostDir = Resolve-Path (Join-Path $Root "native-host")
$ManifestPath = Join-Path $HostDir "$HostName.json"
$HostExe = Join-Path $HostDir "publish\PdfSanitizerHost.exe"
$RepoDotnet = Join-Path $Root ".dotnet\dotnet.exe"
$HasDotnet = (Test-Path $RepoDotnet) -or [bool](Get-Command dotnet -ErrorAction SilentlyContinue)

if (-not $SkipBuild) {
    if ($HasDotnet) {
        & (Join-Path $PSScriptRoot "build_native_host.ps1") -Runtime $Runtime
    } elseif (Test-Path $HostExe) {
        Write-Host "No .NET SDK found. Using existing published native host: $HostExe"
    } else {
        throw ".NET SDK was not found and no published native host exists. Install .NET SDK 8 or newer, or copy native-host\publish\PdfSanitizerHost.exe and rerun with -SkipBuild."
    }
}

if (-not (Test-Path $HostExe)) {
    throw "Native host executable was not found: $HostExe"
}

$AllowedOrigins = $ExtensionId |
    Where-Object { $_ -and $_.Trim() } |
    ForEach-Object {
        $id = $_.Trim()
        if ($id.StartsWith("chrome-extension://")) {
            if ($id.EndsWith("/")) { $id } else { "$id/" }
        } else {
            "chrome-extension://$id/"
        }
    } |
    Select-Object -Unique

if (-not $AllowedOrigins -or $AllowedOrigins.Count -eq 0) {
    throw "At least one Chrome or Edge extension ID is required."
}

$Manifest = [ordered]@{
    name = $HostName
    description = "local-pdf-sanitizer native messaging host"
    path = (Resolve-Path $HostExe).Path
    type = "stdio"
    allowed_origins = @($AllowedOrigins)
}

$Manifest | ConvertTo-Json -Depth 4 | Set-Content -Path $ManifestPath -Encoding UTF8

$RegistryTargets = @()
if ($Browser -eq "Chrome" -or $Browser -eq "Both") {
    if ($Scope -eq "Machine") {
        $RegistryTargets += "HKLM\Software\Google\Chrome\NativeMessagingHosts\$HostName"
    } else {
        $RegistryTargets += "HKCU\Software\Google\Chrome\NativeMessagingHosts\$HostName"
    }
}
if ($Browser -eq "Edge" -or $Browser -eq "Both") {
    if ($Scope -eq "Machine") {
        $RegistryTargets += "HKLM\Software\Microsoft\Edge\NativeMessagingHosts\$HostName"
    } else {
        $RegistryTargets += "HKCU\Software\Microsoft\Edge\NativeMessagingHosts\$HostName"
    }
}

foreach ($Target in $RegistryTargets) {
    & reg.exe ADD $Target /ve /t REG_SZ /d $ManifestPath /f | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to register native messaging host at $Target"
    }
}

Write-Host "Native messaging host manifest written to $ManifestPath"
Write-Host "Allowed origins:"
$AllowedOrigins | ForEach-Object { Write-Host "  $_" }
