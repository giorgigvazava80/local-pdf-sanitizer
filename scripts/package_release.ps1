param(
    [ValidateSet("win-x64", "win-arm64")]
    [string] $Runtime = "win-x64",

    [string] $OutputZip = ""
)

$ErrorActionPreference = "Stop"

$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
$ArtifactsDir = Join-Path $Root "artifacts"
$StageDir = Join-Path $ArtifactsDir "package"

if (-not $OutputZip) {
    $OutputZip = Join-Path $ArtifactsDir "local-pdf-sanitizer-$Runtime.zip"
}

& (Join-Path $PSScriptRoot "build_native_host.ps1") -Runtime $Runtime

Remove-Item $StageDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $OutputZip -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $StageDir | Out-Null

Copy-Item (Join-Path $Root "extension") (Join-Path $StageDir "extension") -Recurse
Copy-Item (Join-Path $Root "native-host") (Join-Path $StageDir "native-host") -Recurse
Copy-Item (Join-Path $Root "scripts") (Join-Path $StageDir "scripts") -Recurse
Copy-Item (Join-Path $Root "docs") (Join-Path $StageDir "docs") -Recurse
Copy-Item (Join-Path $Root "README.md") (Join-Path $StageDir "README.md")
Copy-Item (Join-Path $Root "LICENSE") (Join-Path $StageDir "LICENSE")
Copy-Item (Join-Path $Root "SECURITY.md") (Join-Path $StageDir "SECURITY.md")
Copy-Item (Join-Path $Root "CONTRIBUTING.md") (Join-Path $StageDir "CONTRIBUTING.md")

$Excludes = @(
    "bin",
    "obj",
    "logs",
    "*.log",
    "*.sanitized.tmp",
    "*.original.pdf",
    "*.original-*.pdf",
    "com.pdf_checker.sanitizer.json"
)

foreach ($Pattern in $Excludes) {
    Get-ChildItem -Path $StageDir -Filter $Pattern -Recurse -Force -ErrorAction SilentlyContinue |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
}

Compress-Archive -Path (Join-Path $StageDir "*") -DestinationPath $OutputZip

Write-Host "Release package created at $OutputZip"
