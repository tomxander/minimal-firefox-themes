# Changelog

Versions are the Windows installer's (`Engine.Version`). The themes themselves follow the same history.

## Unreleased

- **Licence** is now the [PolyForm Noncommercial License 1.0.0](LICENSE.md) (briefly MIT before the repository was shared): free to use, change and share for non-commercial purposes, not to sell.
- Minimal Dark: hide the purple "Synchronise your data everywhere" sign-in banner in the ☰ menu (shown to signed-out users). Signing in is still available in Settings › Sync.

## 1.1 (2 November 2026)

[Release](https://github.com/tomxander/firefox-themes/releases/tag/v1.1)

Minimal Dark:

- **One row.** The address bar sits in the tab strip, and ☰ moves to the far right next to the window buttons. Every control is 28px on one centre line, and the current tab pill matches the address field.
- **Sidebar off by default** (`sidebar.visibility` = `hide-sidebar`); Ctrl+B opens it.
- **Sidebar panels** (History, Bookmarks, Synced tabs) styled: compact 26px rows, flat muted section headings, no purple from coloured Firefox themes.
- **Library window** (Manage bookmarks, History, Downloads) styled: graphite panes, no lines, a soft white selection instead of blue.
- **Dialogs, permission prompts, tab previews, private windows, error pages, Firefox View and the Private Browsing page** brought into the same palette.
- **Address dropdown:** flat edge, exactly the field's width, one line per suggestion, compact 12px text.
- **Narrower tabs** (64–168px) and no gap between + and ☰.
- **All accents one device pixel** at any display scaling.
- **Subtle motion** on hover and open, switched off when Windows animations are off.
- **macOS variant** (section 20): ☰ at the right edge, room for the traffic lights, the system font. Not yet checked on a Mac.

Installer:

- Carries the theme above. 106 KB.
- SHA-256 `435958C3BFD8C8B7BD94AFF8C52FE0E8486516BE8562945BF37358156EA34E90`.

## 1.0

The first Windows installer: Minimal Dark in two rows (tabs, then the address bar), with the bottom bookmarks bar, the blank new tab page and pinned sites in the address dropdown. It backed up the profile, kept the person's own settings and added an Installed apps entry with a clean uninstall. 70 KB.
