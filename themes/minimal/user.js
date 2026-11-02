// Lets Firefox load chrome\userChrome.css and chrome\userContent.css (the "Minimal" look).
user_pref("toolkit.legacyUserProfileCustomizations.stylesheets", true);

// Firefox's compact sizing for toolbars and tabs.
user_pref("browser.uidensity", 1);
user_pref("browser.compactmode.show", true);

// Slim scrollbars that appear on hover.
user_pref("widget.non-native-theme.scrollbar.style", 5);
user_pref("ui.useOverlayScrollbars", 1);

// Bookmarks bar on: userChrome.css turns it into the hidden bottom bar that
// slides up when the mouse touches the bottom edge of the window.
user_pref("browser.toolbars.bookmarks.visibility", "always");

// New tab page: a blank sheet (the cursor waits in the address bar).
user_pref("browser.newtabpage.activity-stream.showSearch", false);
// Shortcuts stay on: they also power the pinned/top-site buttons that drop
// down from the address bar when you click into it. (The new tab page itself
// is kept blank by userContent.css.)
user_pref("browser.newtabpage.activity-stream.feeds.topsites", true);
user_pref("browser.urlbar.suggest.topsites", true);
user_pref("browser.newtabpage.activity-stream.feeds.section.topstories", false);
user_pref("browser.newtabpage.activity-stream.showSponsored", false);
user_pref("browser.newtabpage.activity-stream.showSponsoredTopSites", false);
user_pref("browser.newtabpage.activity-stream.showWeather", false);
user_pref("browser.newtabpage.activity-stream.widgets.enabled", false);
user_pref("browser.newtabpage.activity-stream.logowordmark.alwaysVisible", false);
