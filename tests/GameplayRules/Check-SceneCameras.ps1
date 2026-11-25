$files = Get-ChildItem -Path "packet-courier/Assets/_Scenes/*.unity"
$missing = 0
foreach ($f in $files) {
    $txt = [System.IO.File]::ReadAllText($f.FullName)
    if ($txt.Contains("Camera:") -or $txt.Contains("m_TagString: MainCamera")) {
        Write-Host "PASS: $($f.Name) has Camera"
    } else {
        Write-Host "FAIL: $($f.Name) is MISSING Camera!"
        $missing++
    }
}
if ($missing -eq 0) {
    Write-Host "ALL $($files.Count) SCENES HAVE ACTIVE CAMERAS!"
    exit 0
} else {
    Write-Host "$missing scenes missing camera!"
    exit 1
}
