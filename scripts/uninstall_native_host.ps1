param(
    [ValidateSet("Chrome", "Edge", "Both")]
    [string] $Browser = "Both",

    [ValidateSet("User", "Machine")]
    [string] $Scope = "User"
)

$HostName = "com.pdf_checker.sanitizer"
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
    & reg.exe DELETE $Target /f 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Removed $Target"
    } else {
        Write-Host "No registry key found at $Target"
    }
}
