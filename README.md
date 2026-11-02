# Firefox themes

Four looks for Firefox, built as `userChrome.css` / `userContent.css` / `user.js`
sets, plus a switcher that swaps between them.

| Folder | Name | Menu bar | Notes |
|---|---|---|---|
| `themes/xp-classic` | XP Classic | shown | Windows XP look |
| `themes/luna-2026` | Luna 2026 | shown | XP re-imagined for 2026 |
| `themes/luna-mac` | Luna + Mac | shown | Luna 2026 with macOS touches |
| `themes/minimal` | Minimal Dark | hidden (Alt shows it) | Flat, colourless graphite. The main theme; everything below is about it. |

Screenshots of Minimal Dark are in `screenshots/`.

## How a theme is built

Each theme folder holds:

- `userChrome.css`: the browser UI (tabs, address bar, menus, sidebar, Library window, dialogs).
- `userContent.css`: pages Firefox draws itself (new tab, Settings, Add-ons, error pages) and per-site styles.
- `user.js`: the preferences the theme needs (it turns on `toolkit.legacyUserProfileCustomizations.stylesheets`, which lets Firefox load the two CSS files).
- `theme.json`: name, description, whether the menu bar is shown, sort order.

Firefox loads `chrome/userChrome.css` and `chrome/userContent.css` from the profile folder, and `user.js` from the profile root. Changes need a Firefox restart.

## Install

### Windows

```powershell
powershell -ExecutionPolicy Bypass -File scripts\install-windows.ps1
```

Copies the themes and the switcher into the profile's `chrome\themes\` folder, then
opens the switcher so you can pick a theme. After that, run
`chrome\themes\Switch-Theme.ps1` in the profile (or make a shortcut to it) to switch.
The switcher closes Firefox, copies the files, resets theme-managed settings, sets the
menu bar, and relaunches.

### macOS

```bash
bash scripts/install-mac.sh minimal
```

Copies one theme into the default profile and tells you to restart Firefox. There is no
Mac switcher yet (the switcher is a PowerShell/WinForms program); run the script again
with another theme name to change. Pass `--profile <path>` to target a specific profile.

## Minimal Dark design notes

Tokens (in `themes/minimal/userChrome.css`, section 1):

- Surfaces: frame `#1C1C1B`, raised `#262624`, popup `#222220`.
- Text: `#E8E6E1`, muted `#8F8C86`, faint `#5E5C58`. No colour anywhere; accents are
  `rgba(255,255,255,0.2)` at one device pixel (`--m-hair`).
- Type: Segoe UI Variable Text 11.5px in the UI, 13px on Firefox's own pages. Hierarchy by
  weight, not size.
- Radius 4 / 8 / 12. Control height 28px in a 36px top row.

Layout: one row. The address bar sits inside the tab strip, ☰ is moved to the right next to
the window buttons, no toolbar icons. The bookmarks bar is hidden at the bottom edge and slides
up on hover. New tab is a blank page. Pinned/top sites drop down from the address bar when you
click into it.

Firefox internals this relies on (Firefox 159): the Nova address bar (`browser.nova.enabled`),
`moz-*` web components styled only through their CSS custom properties, `@-moz-document
url-prefix("chrome://browser/content/sidebar/")` for the sidebar documents,
`@-moz-document url-prefix("chrome://browser/content/places/places.xhtml")` for the Library.

## Known gaps

- **Mac.** The CSS was written and tested on Windows 11. On a Mac: the font falls back to the
  system font (fine); the window buttons are on the left, so the ☰ placement
  (`#PanelUI-button { position: fixed; right: ... }`, section 13) and the
  `.titlebar-buttonbox-container` margin need a Mac variant; the Windows caption-button and
  Mica rules are harmless. This is the first thing to do for a Mac version.
- **Switcher** is Windows only.
- **Installer.** A native Windows installer (C#, compiled with the .NET Framework `csc.exe`)
  existed; the exe and its sources were lost and it has not been rebuilt. It needs rebuilding
  to carry the current theme.
- **Switcher** reopens Firefox with an extra blank tab after each switch.
- Holding Alt can make the menu bar cover ☰.
- Styled but not yet checked on screen: dialogs, permission prompts, tab previews, error
  pages, Firefox View, private windows.

## Ideas on the list

- Per-site restyles in `userContent.css` (`@-moz-document domain("wikipedia.org") {...}`),
  one file per site, imported from `userContent.css`.
- Installer that also installs chosen add-ons (Return YouTube Dislike, Modern for Wikipedia,
  Enhancer for YouTube) by downloading them from addons.mozilla.org at install time.
