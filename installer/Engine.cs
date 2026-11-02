// Install / uninstall logic for the Minimal Firefox theme. Mirrors the tested
// PowerShell scripts: back up first, keep the person's own settings, re-running
// is safe, uninstall restores exactly what was there before.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace MinimalTheme
{
    public class Engine
    {
        public const string ProductName = "Minimal theme for Firefox";
        public const string Version = "1.1";
        const string BlockStart = "// ----- Minimal Firefox theme: start (added by the installer) -----";
        const string BlockEnd = "// ----- Minimal Firefox theme: end -----";
        const string BackupRoot = "minimal-theme-backup";
        const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\MinimalFirefoxTheme";
        const string XulDoc = "chrome://browser/content/browser.xhtml";

        // Settings the theme sets. Firefox copies user.js values into prefs.js,
        // so uninstall puts these back to what they were before.
        static readonly string[] ThemePrefs = {
            "toolkit.legacyUserProfileCustomizations.stylesheets",
            "browser.uidensity",
            "browser.compactmode.show",
            "widget.non-native-theme.scrollbar.style",
            "ui.useOverlayScrollbars",
            "browser.toolbars.bookmarks.visibility",
            "browser.newtabpage.activity-stream.showSearch",
            "browser.newtabpage.activity-stream.feeds.topsites",
            "browser.urlbar.suggest.topsites",
            "browser.newtabpage.activity-stream.feeds.section.topstories",
            "browser.newtabpage.activity-stream.showSponsored",
            "browser.newtabpage.activity-stream.showSponsoredTopSites",
            "browser.newtabpage.activity-stream.showWeather",
            "browser.newtabpage.activity-stream.widgets.enabled",
            "browser.newtabpage.activity-stream.logowordmark.alwaysVisible"
        };

        static readonly Encoding Utf8 = new UTF8Encoding(false);

        public string Profile { get; private set; }
        public bool Register { get; set; }

        public Engine(string profile) { Profile = profile; Register = true; }

        string LatestFile { get { return Path.Combine(Profile, BackupRoot, "latest.txt"); } }
        public bool IsInstalled { get { return File.Exists(LatestFile); } }

        public string ProfileName
        {
            get
            {
                string n = Path.GetFileName(Profile.TrimEnd('\\'));
                int dot = n.IndexOf('.');
                return dot >= 0 && dot < n.Length - 1 ? n.Substring(dot + 1) : n;
            }
        }

        // ---------- Finding Firefox ----------

        public static string FindProfile()
        {
            string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Mozilla\Firefox");
            string ini = Path.Combine(root, "profiles.ini");
            if (!File.Exists(ini)) return null;

            var sections = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, string> cur = null;
            foreach (string raw in File.ReadAllLines(ini))
            {
                string line = raw.Trim();
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    cur = new Dictionary<string, string>();
                    sections[line.Substring(1, line.Length - 2)] = cur;
                }
                else if (cur != null && line.Contains("="))
                {
                    int i = line.IndexOf('=');
                    cur[line.Substring(0, i)] = line.Substring(i + 1);
                }
            }

            Func<string, string, string> resolve = (path, isRelative) =>
                isRelative == "0" ? path : Path.Combine(root, path.Replace('/', '\\'));

            // Newer Firefox: the [Install...] section names the default profile.
            foreach (var kv in sections)
            {
                string def;
                if (kv.Key.StartsWith("Install") && kv.Value.TryGetValue("Default", out def))
                {
                    string p = resolve(def, "1");
                    if (Directory.Exists(p)) return p;
                }
            }
            // Older Firefox: the profile marked Default=1.
            foreach (var kv in sections)
            {
                string d, path, rel;
                if (kv.Key.StartsWith("Profile") && kv.Value.TryGetValue("Default", out d) && d == "1"
                    && kv.Value.TryGetValue("Path", out path))
                {
                    kv.Value.TryGetValue("IsRelative", out rel);
                    string p = resolve(path, rel ?? "1");
                    if (Directory.Exists(p)) return p;
                }
            }
            return null;
        }

        public static string FirefoxPath()
        {
            try
            {
                using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\firefox.exe"))
                {
                    string p = k == null ? null : k.GetValue("") as string;
                    if (!string.IsNullOrEmpty(p) && File.Exists(p.Trim('"'))) return p.Trim('"');
                }
            }
            catch { }
            foreach (string baseDir in new[] {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) })
            {
                string p = Path.Combine(baseDir, @"Mozilla Firefox\firefox.exe");
                if (File.Exists(p)) return p;
            }
            return null;
        }

        public static bool FirefoxRunning() { return Process.GetProcessesByName("firefox").Length > 0; }

        // Close Firefox the way clicking X does, so open tabs come back.
        public static bool CloseFirefox()
        {
            foreach (var p in Process.GetProcessesByName("firefox"))
            {
                try { if (p.MainWindowHandle != IntPtr.Zero) p.CloseMainWindow(); } catch { }
            }
            for (int i = 0; i < 40; i++)
            {
                Thread.Sleep(500);
                if (!FirefoxRunning()) return true;
            }
            return false;
        }

        public static void StartFirefox()
        {
            string ff = FirefoxPath();
            if (ff != null) Process.Start(ff);
        }

        // ---------- Install ----------

        public void Install(Action<string, double> progress)
        {
            string chrome = Path.Combine(Profile, "chrome");

            // 1. Back up what the install changes. If the theme is already
            //    installed, keep the original backup so uninstall still returns
            //    to the pre-theme Firefox.
            progress("Backing up your current Firefox look", 0.15);
            string existing = IsInstalled ? Path.Combine(Profile, BackupRoot, File.ReadAllText(LatestFile).Trim()) : null;
            if (existing == null || !Directory.Exists(existing))
            {
                string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string backup = Path.Combine(Profile, BackupRoot, stamp);
                Directory.CreateDirectory(backup);
                if (Directory.Exists(chrome)) CopyDir(chrome, Path.Combine(backup, "chrome"));
                foreach (string f in new[] { "user.js", "prefs.js", "xulstore.json" })
                {
                    string p = Path.Combine(Profile, f);
                    if (File.Exists(p)) File.Copy(p, Path.Combine(backup, f), true);
                }
                File.WriteAllText(LatestFile, stamp, Utf8);
            }

            // 2. Theme styles
            progress("Adding the theme", 0.4);
            Directory.CreateDirectory(chrome);
            File.WriteAllText(Path.Combine(chrome, "userChrome.css"), Resource("userChrome.css"), Utf8);
            File.WriteAllText(Path.Combine(chrome, "userContent.css"), Resource("userContent.css"), Utf8);

            // 3. Settings: keep the person's own user.js lines, swap in the theme's block
            progress("Applying the theme's settings", 0.6);
            string userJs = Path.Combine(Profile, "user.js");
            var lines = RemoveBlock(ReadLines(userJs)).Where(l => !IsThemePref(l)).ToList();
            lines.Add(BlockStart);
            lines.AddRange(Resource("user.js").Replace("\r\n", "\n").TrimEnd('\n').Split('\n'));
            lines.Add(BlockEnd);
            WriteLines(userJs, lines);

            // 4. Menu bar hidden (hold Alt to show it)
            progress("Hiding the menu bar", 0.8);
            SetMenuBarAutohide("true");

            if (Register) { progress("Adding it to Installed apps", 0.9); RegisterUninstall(); }
            progress("Done", 1.0);
        }

        // ---------- Uninstall ----------

        public void Uninstall(Action<string, double> progress)
        {
            if (!IsInstalled) { if (Register) UnregisterUninstall(); return; }
            string backup = Path.Combine(Profile, BackupRoot, File.ReadAllText(LatestFile).Trim());
            string chrome = Path.Combine(Profile, "chrome");

            // 1. Styles: remove the theme's, put back any the person had
            progress("Removing the theme", 0.2);
            foreach (string f in new[] { "userChrome.css", "userContent.css" })
            {
                string p = Path.Combine(chrome, f);
                if (File.Exists(p)) File.Delete(p);
            }
            string oldChrome = Path.Combine(backup, "chrome");
            if (Directory.Exists(oldChrome)) CopyDir(oldChrome, chrome);
            else if (Directory.Exists(chrome) && !Directory.EnumerateFileSystemEntries(chrome).Any()) Directory.Delete(chrome);

            // 2. user.js: drop the theme's block, restore the person's own lines for those settings
            progress("Restoring your settings", 0.45);
            string userJs = Path.Combine(Profile, "user.js");
            var restored = RemoveBlock(ReadLines(Path.Combine(backup, "user.js"))).Where(IsThemePref);
            var lines = RemoveBlock(ReadLines(userJs)).Concat(restored).ToList();
            if (lines.Any(l => l.Trim().Length > 0)) WriteLines(userJs, lines);
            else if (File.Exists(userJs)) File.Delete(userJs);

            // 3. prefs.js: put the old values of the theme's settings back
            string prefsJs = Path.Combine(Profile, "prefs.js");
            if (File.Exists(prefsJs))
            {
                var current = ReadLines(prefsJs).Where(l => !IsThemePref(l));
                var old = ReadLines(Path.Combine(backup, "prefs.js")).Where(IsThemePref);
                WriteLines(prefsJs, current.Concat(old).ToList());
            }

            // 4. Menu bar back to how it was
            progress("Restoring the menu bar", 0.7);
            SetMenuBarAutohide(ReadMenuBarAutohide(Path.Combine(backup, "xulstore.json")));

            File.Delete(LatestFile);
            if (Register) { progress("Removing it from Installed apps", 0.9); UnregisterUninstall(); }
            progress("Done", 1.0);
        }

        // ---------- Installed apps entry ----------

        public static string InstallDir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Minimal Firefox Theme"); }
        }

        void RegisterUninstall()
        {
            Directory.CreateDirectory(InstallDir);
            string self = Assembly.GetExecutingAssembly().Location;
            string target = Path.Combine(InstallDir, "Minimal Firefox Theme.exe");
            if (!string.Equals(Path.GetFullPath(self), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
                File.Copy(self, target, true);
            using (var k = Registry.CurrentUser.CreateSubKey(UninstallKey))
            {
                k.SetValue("DisplayName", ProductName);
                k.SetValue("DisplayVersion", Version);
                k.SetValue("DisplayIcon", target);
                k.SetValue("InstallLocation", InstallDir);
                k.SetValue("UninstallString", "\"" + target + "\" /uninstall");
                k.SetValue("QuietUninstallString", "\"" + target + "\" /uninstall /silent");
                k.SetValue("NoModify", 1, RegistryValueKind.DWord);
                k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                k.SetValue("EstimatedSize", (int)Math.Max(1, new FileInfo(target).Length / 1024), RegistryValueKind.DWord);
                k.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
            }
        }

        static void UnregisterUninstall()
        {
            try { Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false); } catch { }
        }

        // Removes the installed copy of this program after it exits.
        public static void ScheduleSelfRemoval()
        {
            if (!Directory.Exists(InstallDir)) return;
            var psi = new ProcessStartInfo("cmd.exe",
                "/c ping 127.0.0.1 -n 3 > nul & rmdir /s /q \"" + InstallDir + "\"")
            { CreateNoWindow = true, UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden };
            Process.Start(psi);
        }

        // ---------- Helpers ----------

        static string Resource(string name)
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("theme." + name))
            using (var r = new StreamReader(s, Encoding.UTF8))
                return r.ReadToEnd();
        }

        static List<string> ReadLines(string path)
        {
            return File.Exists(path) ? File.ReadAllLines(path, Encoding.UTF8).ToList() : new List<string>();
        }

        static void WriteLines(string path, List<string> lines)
        {
            File.WriteAllText(path, string.Join("\r\n", lines) + "\r\n", Utf8);
        }

        static List<string> RemoveBlock(IEnumerable<string> lines)
        {
            var outp = new List<string>();
            bool inBlock = false;
            foreach (string l in lines)
            {
                if (l == BlockStart) { inBlock = true; continue; }
                if (l == BlockEnd) { inBlock = false; continue; }
                if (!inBlock) outp.Add(l);
            }
            return outp;
        }

        static bool IsThemePref(string line)
        {
            foreach (string p in ThemePrefs)
                if (line.StartsWith("user_pref(\"" + p + "\",")) return true;
            return false;
        }

        static void CopyDir(string from, string to)
        {
            Directory.CreateDirectory(to);
            foreach (string f in Directory.GetFiles(from)) File.Copy(f, Path.Combine(to, Path.GetFileName(f)), true);
            foreach (string d in Directory.GetDirectories(from)) CopyDir(d, Path.Combine(to, Path.GetFileName(d)));
        }

        // xulstore.json remembers whether the menu bar is shown. null = remove the entry.
        void SetMenuBarAutohide(string value)
        {
            string xul = Path.Combine(Profile, "xulstore.json");
            var json = new JavaScriptSerializer();
            var data = File.Exists(xul)
                ? (json.DeserializeObject(File.ReadAllText(xul)) as Dictionary<string, object>) ?? new Dictionary<string, object>()
                : new Dictionary<string, object>();
            object docObj;
            Dictionary<string, object> doc;
            if (data.TryGetValue(XulDoc, out docObj) && docObj is Dictionary<string, object>) doc = (Dictionary<string, object>)docObj;
            else { doc = new Dictionary<string, object>(); data[XulDoc] = doc; }

            object barObj;
            Dictionary<string, object> bar = doc.TryGetValue("toolbar-menubar", out barObj) ? barObj as Dictionary<string, object> : null;
            if (value == null)
            {
                if (bar != null) { bar.Remove("autohide"); if (bar.Count == 0) doc.Remove("toolbar-menubar"); }
            }
            else
            {
                if (bar == null) { bar = new Dictionary<string, object>(); doc["toolbar-menubar"] = bar; }
                bar["autohide"] = value;
            }
            File.WriteAllText(xul, json.Serialize(data), Utf8);
        }

        static string ReadMenuBarAutohide(string xulPath)
        {
            if (!File.Exists(xulPath)) return null;
            try
            {
                var data = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(xulPath)) as Dictionary<string, object>;
                object doc, bar, v;
                if (data != null && data.TryGetValue(XulDoc, out doc) && doc is Dictionary<string, object>
                    && ((Dictionary<string, object>)doc).TryGetValue("toolbar-menubar", out bar) && bar is Dictionary<string, object>
                    && ((Dictionary<string, object>)bar).TryGetValue("autohide", out v))
                    return v as string;
            }
            catch { }
            return null;
        }
    }
}
