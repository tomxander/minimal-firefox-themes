<#
  Install the themes and the switcher into the default Firefox profile on Windows,
  then open the switcher.
    powershell -ExecutionPolicy Bypass -File scripts\install-windows.ps1
    powershell -ExecutionPolicy Bypass -File scripts\install-windows.ps1 -Profile "C:\...\Profiles\xxxx.default-release"
#>
param([string]$Profile)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent

if (-not $Profile) {
  $base = Join-Path $env:APPDATA 'Mozilla\Firefox\Profiles'
  $Profile = (Get-ChildItem $base -Directory | Where-Object Name -like '*.default-release*' | Select-Object -First 1).FullName
  if (-not $Profile) { $Profile = (Get-ChildItem $base -Directory | Where-Object Name -like '*.default*' | Select-Object -First 1).FullName }
}
if (-not $Profile -or -not (Test-Path (Join-Path $Profile 'prefs.js'))) {
  throw "No Firefox profile found. Pass -Profile <path> (see about:profiles in Firefox)."
}

$dest = Join-Path $Profile 'chrome\themes'
New-Item -ItemType Directory -Force $dest | Out-Null
Get-ChildItem (Join-Path $repo 'themes') -Directory | ForEach-Object {
  Copy-Item $_.FullName (Join-Path $dest $_.Name) -Recurse -Force
}
Copy-Item (Join-Path $repo 'scripts\Switch-Theme.ps1') (Join-Path $dest 'Switch-Theme.ps1') -Force

"Themes installed into $dest"
"Opening the switcher..."
& (Join-Path $dest 'Switch-Theme.ps1')
