<#
  Firefox theme switcher.
  Each sub-folder here is a theme: userChrome.css, userContent.css, user.js and theme.json.

  Run with no arguments for the picker window, or from a console:
    Switch-Theme.ps1 -Theme minimal            apply a theme (closes and reopens Firefox)
    Switch-Theme.ps1 -Theme minimal -DryRun    show what would change, change nothing
    Switch-Theme.ps1 -List                     list themes
#>
param(
  [string]$Theme,
  [switch]$DryRun,
  [switch]$List
)

$ErrorActionPreference = 'Stop'
$ThemesDir  = $PSScriptRoot
$ChromeDir  = Split-Path $ThemesDir -Parent
$ProfileDir = Split-Path $ChromeDir -Parent
$CurrentFile = Join-Path $ThemesDir 'current.txt'
$Utf8 = New-Object System.Text.UTF8Encoding $false

# Settings the themes manage. They are cleared from prefs.js on every switch so a
# theme that doesn't set them gets Firefox's defaults back.
$ManagedPrefs = @(
  'browser.uidensity',
  'sidebar.visibility',
  'widget.non-native-theme.scrollbar.style',
  'ui.useOverlayScrollbars',
  'browser.toolbars.bookmarks.visibility',
  'browser.newtabpage.activity-stream.showSearch',
  'browser.newtabpage.activity-stream.feeds.topsites',
  'browser.urlbar.suggest.topsites',
  'browser.newtabpage.activity-stream.feeds.section.topstories',
  'browser.newtabpage.activity-stream.showSponsored',
  'browser.newtabpage.activity-stream.showSponsoredTopSites',
  'browser.newtabpage.activity-stream.showWeather',
  'browser.newtabpage.activity-stream.widgets.enabled',
  'browser.newtabpage.activity-stream.logowordmark.alwaysVisible'
)

function Get-Themes {
  Get-ChildItem $ThemesDir -Directory | ForEach-Object {
    $metaPath = Join-Path $_.FullName 'theme.json'
    if (Test-Path $metaPath) {
      $meta = Get-Content $metaPath -Raw | ConvertFrom-Json
      [pscustomobject]@{
        Id          = $_.Name
        Name        = $meta.name
        Description = $meta.description
        MenuBar     = [bool]$meta.menubar
        Order       = [int]$meta.order
        Path        = $_.FullName
      }
    }
  } | Sort-Object Order
}

function Get-CurrentThemeId {
  if (Test-Path $CurrentFile) { (Get-Content $CurrentFile -Raw).Trim() } else { '' }
}

function Get-FirefoxPath {
  $key = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\firefox.exe'
  try { $p = (Get-ItemProperty $key -ErrorAction Stop).'(default)'; if ($p -and (Test-Path $p)) { return $p } } catch {}
  return 'C:\Program Files\Mozilla Firefox\firefox.exe'
}

# Close Firefox the same way clicking X does, so the session is saved and restored.
function Close-Firefox {
  $procs = Get-Process firefox -ErrorAction SilentlyContinue
  if (-not $procs) { return $true }
  $procs | Where-Object { $_.MainWindowHandle -ne 0 } | ForEach-Object { [void]$_.CloseMainWindow() }
  for ($i = 0; $i -lt 40; $i++) {
    Start-Sleep -Milliseconds 500
    if (-not (Get-Process firefox -ErrorAction SilentlyContinue)) { return $true }
  }
  return $false
}

function Set-MenuBar([bool]$visible) {
  $xul = Join-Path $ProfileDir 'xulstore.json'
  $data = if (Test-Path $xul) { Get-Content $xul -Raw | ConvertFrom-Json } else { New-Object psobject }
  $docKey = 'chrome://browser/content/browser.xhtml'
  if (-not $data.PSObject.Properties[$docKey]) { $data | Add-Member -NotePropertyName $docKey -NotePropertyValue (New-Object psobject) }
  $doc = $data.$docKey
  # Firefox stores "attribute removed" as this marker; removed autohide = menu bar always shown.
  $value = if ($visible) { "-moz-missing`n" } else { 'true' }
  if ($doc.PSObject.Properties['toolbar-menubar']) {
    $doc.'toolbar-menubar' | Add-Member -NotePropertyName autohide -NotePropertyValue $value -Force
  } else {
    $doc | Add-Member -NotePropertyName 'toolbar-menubar' -NotePropertyValue ([pscustomobject]@{ autohide = $value })
  }
  [IO.File]::WriteAllText($xul, ($data | ConvertTo-Json -Depth 20 -Compress), $Utf8)
}

function Clear-ManagedPrefs([bool]$restoreBookmarksBar) {
  $prefsPath = Join-Path $ProfileDir 'prefs.js'
  if (-not (Test-Path $prefsPath)) { return }
  $lines = [IO.File]::ReadAllLines($prefsPath)
  $kept = $lines | Where-Object {
    $line = $_
    -not ($ManagedPrefs | Where-Object { $line -like "user_pref(`"$_`",*" })
  }
  if ($restoreBookmarksBar) { $kept += 'user_pref("browser.toolbars.bookmarks.visibility", "always");' }
  [IO.File]::WriteAllLines($prefsPath, [string[]]$kept, $Utf8)
}

function Apply-Theme($t) {
  $prefsStep = 'Reset theme-managed settings in prefs.js'
  if ($t.MenuBar) { $prefsStep += ' (bookmarks bar back on)' }
  $menuStep = if ($t.MenuBar) { 'Menu bar: always shown' } else { 'Menu bar: hidden (Alt shows it)' }
  $steps = @(
    "Copy $($t.Id)\userChrome.css and userContent.css into chrome\",
    "Copy $($t.Id)\user.js into the profile",
    $prefsStep,
    $menuStep
  )
  if ($DryRun) { "Dry run - would apply '$($t.Name)':"; $steps | ForEach-Object { "  - $_" }; return $true }

  if (-not (Close-Firefox)) { throw 'Firefox did not close. Close it yourself (check for a "close tabs?" prompt) and try again.' }

  Copy-Item (Join-Path $t.Path 'userChrome.css') (Join-Path $ChromeDir 'userChrome.css') -Force
  Copy-Item (Join-Path $t.Path 'userContent.css') (Join-Path $ChromeDir 'userContent.css') -Force
  Copy-Item (Join-Path $t.Path 'user.js') (Join-Path $ProfileDir 'user.js') -Force
  Clear-ManagedPrefs $t.MenuBar
  Set-MenuBar $t.MenuBar
  [IO.File]::WriteAllText($CurrentFile, $t.Id, $Utf8)

  Start-Process (Get-FirefoxPath)
  return $true
}

# ---------- Console use ----------
$themes = @(Get-Themes)
if ($List) {
  $current = Get-CurrentThemeId
  $themes | ForEach-Object { '{0}{1,-12} {2}' -f $(if ($_.Id -eq $current) { '* ' } else { '  ' }), $_.Id, $_.Name }
  return
}
if ($Theme) {
  $t = $themes | Where-Object { $_.Id -eq $Theme -or $_.Name -eq $Theme } | Select-Object -First 1
  if (-not $t) { throw "No theme called '$Theme'. Themes: $($themes.Id -join ', ')" }
  $out = Apply-Theme $t
  if ($DryRun) { $out | Where-Object { $_ -is [string] } } else { "Applied '$($t.Name)'." }
  return
}

# ---------- Picker window ----------
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

$font     = New-Object System.Drawing.Font('Segoe UI', 10)
$fontBold = New-Object System.Drawing.Font('Segoe UI Semibold', 11)

$form = New-Object System.Windows.Forms.Form
$form.Text = 'Firefox Themes'
$form.ClientSize = New-Object System.Drawing.Size(460, 400)
$form.StartPosition = 'CenterScreen'
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false
$form.Font = $font
$form.BackColor = [System.Drawing.Color]::White

$title = New-Object System.Windows.Forms.Label
$title.Text = 'Choose a look for Firefox'
$title.Font = $fontBold
$title.AutoSize = $true
$title.Location = New-Object System.Drawing.Point(20, 16)
$form.Controls.Add($title)

$current = Get-CurrentThemeId
$y = 52
$radios = @()
foreach ($t in $themes) {
  $rb = New-Object System.Windows.Forms.RadioButton
  $rb.Text = $t.Name + $(if ($t.Id -eq $current) { '   (current)' } else { '' })
  $rb.Font = $fontBold
  $rb.Tag = $t
  $rb.AutoSize = $true
  $rb.Location = New-Object System.Drawing.Point(20, $y)
  $rb.Checked = ($t.Id -eq $current)
  $form.Controls.Add($rb)
  $radios += $rb

  $desc = New-Object System.Windows.Forms.Label
  $desc.Text = $t.Description
  $desc.ForeColor = [System.Drawing.Color]::FromArgb(95, 97, 112)
  $desc.Location = New-Object System.Drawing.Point(38, ($y + 26))
  $desc.Size = New-Object System.Drawing.Size(400, 36)
  $form.Controls.Add($desc)
  $y += 72
}
if (-not ($radios | Where-Object Checked) -and $radios) { $radios[0].Checked = $true }

$note = New-Object System.Windows.Forms.Label
$note.Text = 'Firefox will close and reopen; your tabs come back.'
$note.ForeColor = [System.Drawing.Color]::FromArgb(95, 97, 112)
$note.AutoSize = $true
$note.Location = New-Object System.Drawing.Point(20, ($y + 8))
$form.Controls.Add($note)

$apply = New-Object System.Windows.Forms.Button
$apply.Text = 'Apply'
$apply.Size = New-Object System.Drawing.Size(100, 32)
$apply.Location = New-Object System.Drawing.Point(340, ($y + 2))
$apply.Add_Click({
  $chosen = ($radios | Where-Object Checked | Select-Object -First 1).Tag
  if (-not $chosen) { return }
  $form.Cursor = 'WaitCursor'
  $apply.Enabled = $false
  try {
    Apply-Theme $chosen | Out-Null
    $form.Close()
  } catch {
    [System.Windows.Forms.MessageBox]::Show($_.Exception.Message, 'Firefox Themes', 'OK', 'Warning') | Out-Null
    $apply.Enabled = $true
  } finally {
    $form.Cursor = 'Default'
  }
})
$form.Controls.Add($apply)
$form.AcceptButton = $apply
$form.ClientSize = New-Object System.Drawing.Size(460, ($y + 50))

[void]$form.ShowDialog()
