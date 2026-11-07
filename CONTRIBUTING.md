# Working on the themes

## The edit loop

1. Edit the files in `themes/<name>/`.
2. Copy them into your Firefox profile: `userChrome.css` and `userContent.css` go into `<profile>\chrome\`, and `user.js` goes into `<profile>\`. Running `scripts\install-windows.ps1` and then `Switch-Theme.ps1 -Theme <name>` does both and restarts Firefox.
3. Restart Firefox. It reads these files only at startup.

To find a profile folder, open `about:profiles` and use **Root Directory**.

## Inspecting Firefox's own UI

Use the **Browser Toolbox**: in `about:config`, set `devtools.chrome.enabled` and `devtools.debugger.remote-enabled` to `true`, then press <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>I</kbd>. It inspects the browser window the way DevTools inspects a page. To keep a menu open while you inspect it, turn on **Disable popup auto-hide** in the Toolbox's ⋯ menu.

## Rules Minimal Dark follows

- **Use the tokens** at the top of `userChrome.css` (`--m-frame`, `--m-raised`, `--m-popup`, `--m-text`, `--m-muted`, `--m-faint`, `--m-accent`). Don't add new colours. The theme has no hue anywhere.
- **Accents are one device pixel**: use `var(--m-hair)` for widths, never `1px`.
- **No divider lines.** Separate things with space or a change of surface.
- **Controls are 28px** (`--m-control`) in a 36px row (`--m-row`).
- **Two type sizes**: 11.5px in the browser and 13px on Firefox's pages. Show hierarchy with weight.
- **Motion** goes inside `@media (prefers-reduced-motion: no-preference)`.
- **Put new rules in the section they belong to**, or append a new numbered section with a heading comment that says what it's for and why.
- **Firefox's web components** (`moz-card`, `moz-page-nav`, `moz-button`…) keep their insides in a shadow DOM. Style them through the CSS variables they read (`--card-*`, `--page-nav-*`, `--button-*`), not through selectors.

## Before you open a pull request

- [ ] Restarted Firefox and checked the main window, the ☰ menu, the address dropdown, a right-click menu and Settings.
- [ ] Checked on a display scaled above 100% if accents or sizes changed.
- [ ] If `user.js` gained a preference, added it to `$ManagedPrefs` in `scripts/Switch-Theme.ps1`, so switching to another theme clears it.
- [ ] If the installer changed, ran `installer\build.ps1` and an install/uninstall round trip against a scratch profile:
      `"Minimal Firefox Theme Setup.exe" /silent /profile "C:\temp\scratch" /nolaunch /noregister`, then the same with `/uninstall`.
- [ ] Added a line to `CHANGELOG.md` under **Unreleased**.

## Releasing an installer

1. Bump `Engine.Version` in `installer/Engine.cs` and the two assembly versions in `installer/Program.cs`.
2. Run `installer\build.ps1`, and note the size and SHA-256.
3. Move the **Unreleased** notes in `CHANGELOG.md` under the new version, with the SHA-256.
4. Tag `vX.Y`, push, and create a GitHub release with `Minimal Firefox Theme Setup.exe` and `SHA256SUMS.txt` attached.
5. Update the download on <https://thomasalex.in/minimal>. Give the file a new name there, because `/assets/` is cached for a year.
