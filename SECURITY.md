# Security

## What the installer touches

`Minimal Firefox Theme Setup.exe` runs as the current user and needs no admin rights. It changes only these:

| Where | What |
|---|---|
| `<Firefox profile>\minimal-theme-backup\` | Before anything changes, a copy of the profile's `chrome` folder, `user.js`, `prefs.js` and `xulstore.json`. |
| `<Firefox profile>\chrome\userChrome.css`, `userContent.css` | The theme. |
| `<Firefox profile>\user.js` | The theme's preferences, in a marked block. Your own lines are kept. |
| `<Firefox profile>\xulstore.json` | Hides the menu bar (<kbd>Alt</kbd> shows it). |
| `%LOCALAPPDATA%\Programs\Minimal Firefox Theme\` | A copy of the installer, used by Uninstall. |
| `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\MinimalFirefoxTheme` | The Installed apps entry. |

Uninstall puts the backed-up `chrome` folder back, removes the marked block from `user.js` (restoring your own values for those settings), restores the menu bar, and deletes the installer copy and the registry entry. If Firefox is open, the installer closes it the way the X button does, and reopens it afterwards.

It doesn't download anything, run anything in Firefox, or send any data anywhere. The themes are CSS and preferences only: no scripts and no extensions.

## Checking a download

The installer isn't code-signed. Before running it, compare its SHA-256 with the one listed in the release notes and in `SHA256SUMS.txt`:

```powershell
Get-FileHash "Minimal Firefox Theme Setup.exe" -Algorithm SHA256
```

You can also build it yourself from `installer/` with `installer\build.ps1`.

## Reporting a problem

If you find a security problem in the installer or the scripts, please don't open a public issue. Get in touch through the contact details at <https://thomasalex.in> instead.
