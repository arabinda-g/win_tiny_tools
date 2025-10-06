# PowerShell script to embed ScreenDimmer icon as Base64 in the resource file

$iconPath = "modules\ScreenDimmer\icon.ico"
$resourceFile = "modules\ScreenDimmer\ScreenDimmerResources.cs"

if (-not (Test-Path $iconPath)) {
    Write-Host "ERROR: ScreenDimmer icon not found at $iconPath"
    exit 1
}

Write-Host "Converting ScreenDimmer icon to Base64..."

# Read icon file and convert to Base64
$iconBytes = [System.IO.File]::ReadAllBytes($iconPath)
$iconBase64 = [System.Convert]::ToBase64String($iconBytes)

Write-Host "Icon converted. Size: $($iconBytes.Length) bytes"

# Read the current resource file
$resourceContent = Get-Content $resourceFile -Raw

# Replace the empty Base64 string with the actual icon data
$newResourceContent = $resourceContent -replace 'return "";', "return `"$iconBase64`";"

# Write the updated resource file
Set-Content -Path $resourceFile -Value $newResourceContent -Encoding UTF8

Write-Host "✓ ScreenDimmer icon embedded in resource file"
Write-Host "✓ Resource file updated: $resourceFile"