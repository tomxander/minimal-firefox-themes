// Lets Firefox load chrome\userChrome.css and chrome\userContent.css (the "Luna 2026" look).
user_pref("toolkit.legacyUserProfileCustomizations.stylesheets", true);
// Slim modern scrollbars that appear on hover (Windows 11 style).
// For XP Classic arrows instead, change 5 to 4 and 1 to 0.
user_pref("widget.non-native-theme.scrollbar.style", 5);
user_pref("ui.useOverlayScrollbars", 1);