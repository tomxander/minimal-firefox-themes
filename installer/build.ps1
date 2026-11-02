<#
  Build "Minimal Firefox Theme Setup.exe" from the Minimal theme. Windows only:
  uses the C# compiler that ships with .NET Framework (no download needed).
    powershell -ExecutionPolicy Bypass -File installer\build.ps1
  Output: installer\dist\Minimal Firefox Theme Setup.exe, plus its SHA-256.
#>
$ErrorActionPreference = 'Stop'
$dir = $PSScriptRoot
$theme = Join-Path (Split-Path $dir -Parent) 'themes\minimal'
$fw = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
$out = Join-Path $dir 'dist'
New-Item -ItemType Directory -Force $out | Out-Null
$exe = Join-Path $out 'Minimal Firefox Theme Setup.exe'

& "$fw\csc.exe" /nologo /target:winexe /optimize+ /out:"$exe" /win32icon:"$dir\icon.ico" `
  /r:System.Web.Extensions.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
  /resource:"$theme\userChrome.css,theme.userChrome.css" `
  /resource:"$theme\userContent.css,theme.userContent.css" `
  /resource:"$theme\user.js,theme.user.js" `
  "$dir\Engine.cs" "$dir\Program.cs"
if ($LASTEXITCODE -ne 0) { throw "csc failed ($LASTEXITCODE)" }

"Built: $exe"
"Size:  " + [math]::Round((Get-Item $exe).Length / 1KB) + " KB"
"SHA-256: " + (Get-FileHash $exe -Algorithm SHA256).Hash
