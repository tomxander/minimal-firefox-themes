// The installer window: styled with the theme's own tokens (graphite, warm
// text, one soft hairline), so it previews what it installs.
//
// Command line (for testing and scripted use):
//   /uninstall         remove instead of install
//   /silent            no window; exit code 0 = success
//   /profile <path>    use this Firefox profile folder instead of the default
//   /nolaunch          don't reopen Firefox afterwards
//   /noregister        don't add/remove the Installed apps entry
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Minimal theme for Firefox")]
[assembly: AssemblyProduct("Minimal theme for Firefox")]
[assembly: AssemblyDescription("Installs the Minimal theme for Firefox")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

namespace MinimalTheme
{
    static class Palette
    {
        public static readonly Color Frame = Color.FromArgb(0x1C, 0x1C, 0x1B);
        public static readonly Color Raised = Color.FromArgb(0x26, 0x26, 0x24);
        public static readonly Color RaisedHover = Color.FromArgb(0x2E, 0x2E, 0x2C);
        public static readonly Color Text = Color.FromArgb(0xE8, 0xE6, 0xE1);
        public static readonly Color Muted = Color.FromArgb(0x8F, 0x8C, 0x86);
        public static readonly Color Track = Color.FromArgb(0x2C, 0x2C, 0x2A);
        public static readonly Color Hairline = Color.FromArgb(0x49, 0x49, 0x47);   // 20% white on graphite
        public static readonly Color HairlineStrong = Color.FromArgb(0xA9, 0xA6, 0xA0);
    }

    static class Ui
    {
        public static float Scale = 1f;
        public static int S(float v) { return (int)Math.Round(v * Scale); }

        static string Family(params string[] names)
        {
            var installed = new InstalledFontCollection().Families.Select(f => f.Name).ToList();
            foreach (string n in names) if (installed.Contains(n)) return n;
            return "Segoe UI";
        }
        static string textFamily, displayFamily;
        public static Font Body(float px, FontStyle style = FontStyle.Regular)
        {
            if (textFamily == null) textFamily = Family("Segoe UI Variable Text", "Segoe UI");
            return new Font(textFamily, px * Scale, style, GraphicsUnit.Pixel);
        }
        public static Font Display(float px)
        {
            if (displayFamily == null) displayFamily = Family("Segoe UI Variable Display Semil", "Segoe UI Variable Display", "Segoe UI Semilight", "Segoe UI");
            return new Font(displayFamily, px * Scale, FontStyle.Regular, GraphicsUnit.Pixel);
        }
        public static Font Icons(float px)
        {
            return new Font(Family("Segoe Fluent Icons", "Segoe MDL2 Assets"), px * Scale, FontStyle.Regular, GraphicsUnit.Pixel);
        }

        public static GraphicsPath Round(Rectangle r, int radius)
        {
            var p = new GraphicsPath();
            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    // Flat rounded button. Primary: raised graphite with a hairline edge. Plain: text only.
    class FlatButton : Control
    {
        public bool Primary;
        bool hover, down;

        public FlatButton(string text, bool primary)
        {
            Text = text; Primary = primary;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Font = Ui.Body(12.5f);
            Size = new Size(Ui.S(Math.Max(96, TextRenderer.MeasureText(text, Font).Width / Ui.Scale + 36)), Ui.S(34));
            TabStop = true;
            Cursor = Cursors.Default;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Focus(); Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override bool IsInputKey(Keys k) { return k == Keys.Enter || k == Keys.Space || base.IsInputKey(k); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) OnClick(EventArgs.Empty);
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Palette.Frame);
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Ui.Round(r, Ui.S(8)))
            {
                Color bg = Primary ? (down ? Palette.Track : hover ? Palette.RaisedHover : Palette.Raised)
                                   : (down ? Palette.Raised : hover ? Palette.Raised : Palette.Frame);
                using (var b = new SolidBrush(bg)) g.FillPath(b, path);
                Color edge = Focused ? Palette.HairlineStrong : Primary ? Palette.Hairline : Color.Empty;
                if (edge != Color.Empty) using (var pen = new Pen(edge, 1f)) g.DrawPath(pen, path);
            }
            Color fg = Primary || hover || Focused ? Palette.Text : Palette.Muted;
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }

    // The single hairline progress bar: the theme's signature line.
    class Hairline : Control
    {
        double value;
        public double Value { get { return value; } set { this.value = Math.Max(0, Math.Min(1, value)); Invalidate(); } }
        public Hairline()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            Height = Math.Max(1, Ui.S(1));
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Palette.Track);
            using (var b = new SolidBrush(Palette.HairlineStrong))
                e.Graphics.FillRectangle(b, 0, 0, (int)(Width * value), Height);
        }
    }

    class SetupForm : Form
    {
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);

        readonly Engine engine;
        readonly bool startWithRemove, testProfile, noLaunch;
        readonly Panel body;
        bool closedFirefox, removedTheme;

        public SetupForm(Engine engine, bool startWithRemove, bool testProfile, bool noLaunch)
        {
            this.engine = engine;
            this.startWithRemove = startWithRemove;
            this.testProfile = testProfile;
            this.noLaunch = noLaunch;

            Text = Engine.ProductName;
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Palette.Frame;
            ForeColor = Palette.Text;
            ClientSize = new Size(Ui.S(520), Ui.S(348));
            KeyPreview = true;
            DoubleBuffered = true;

            // Header: app name (drag the window from here) and close
            var name = new Label
            {
                Text = "Minimal for Firefox", AutoSize = true, ForeColor = Palette.Muted, BackColor = Palette.Frame,
                Font = Ui.Body(12f), Location = new Point(Ui.S(32), Ui.S(17))
            };
            var close = new FlatButton("\uE8BB", false) { Font = Ui.Icons(10f), Size = new Size(Ui.S(40), Ui.S(32)), Location = new Point(Ui.S(472), Ui.S(8)), TabStop = false };
            close.Click += (s, e) => Close();
            foreach (Control c in new Control[] { this, name }) c.MouseDown += Drag;
            Controls.Add(name); Controls.Add(close);

            body = new Panel { Location = new Point(Ui.S(32), Ui.S(56)), Size = new Size(Ui.S(456), Ui.S(276)), BackColor = Palette.Frame };
            body.MouseDown += Drag;
            Controls.Add(body);

            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };
            FormClosed += (s, e) => { if (removedTheme && !testProfile) Engine.ScheduleSelfRemoval(); };
            Load += (s, e) => Start();
        }

        protected override CreateParams CreateParams
        {
            get { var cp = base.CreateParams; cp.ClassStyle |= 0x20000; /* drop shadow */ return cp; }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int round = 2, dark = 1;
            try
            {
                DwmSetWindowAttribute(Handle, 33, ref round, 4);   // Windows 11 rounded corners
                DwmSetWindowAttribute(Handle, 20, ref dark, 4);    // dark window frame
            }
            catch { }
        }

        void Drag(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero);
        }

        // ---------- Screens ----------

        void Start()
        {
            if (engine == null)
                Page("Firefox isn't set up yet",
                     "Open Firefox once so it can create your profile, close it, then run this again.",
                     null, Btn("Close", true, Close));
            else if (startWithRemove)
                ConfirmRemove();
            else if (engine.IsInstalled)
                Page("Minimal is installed",
                     "Reinstall to refresh the theme, for example after a Firefox update. Or remove it to put Firefox back exactly as it was.",
                     Details(),
                     Btn("Reinstall", true, () => Run(true)), Btn("Remove", false, () => Run(false)));
            else
                Page("A quieter Firefox",
                     "Minimal turns Firefox into two slim rows, your tabs and the address bar, on a flat graphite frame. "
                     + "New tabs open as a blank page, and your bookmarks wait just below the bottom edge of the window.",
                     Details(),
                     Btn("Install", true, () => Run(true)), Btn("Cancel", false, Close));
        }

        void ConfirmRemove()
        {
            Page("Remove Minimal?",
                 "Firefox goes back to exactly how it looked before the theme was installed. Your bookmarks, history and passwords aren't touched.",
                 null,
                 Btn("Remove", true, () => Run(false)), Btn("Cancel", false, Close));
        }

        string[,] Details()
        {
            return new string[,] {
                { "Installs into", "Your Firefox profile \u201C" + engine.ProfileName + "\u201D" },
                { "Firefox", "Closes and reopens. Your open tabs come back." },
                { "To undo", "Settings \u203A Apps \u203A Installed apps" }
            };
        }

        void Run(bool install)
        {
            if (!testProfile && Engine.FirefoxRunning()) { NeedsClose(install); return; }
            Work(install, false);
        }

        void NeedsClose(bool install)
        {
            Page("Firefox needs to close",
                 "Firefox closes for a moment while its look changes. Your open tabs come back when it reopens.",
                 null,
                 Btn("Close Firefox and continue", true, () => Work(install, true)),
                 Btn("Cancel", false, Start));
        }

        // Draws every screen to PNG files (for reviewing the design): /render <folder>
        public void RenderScreens(string dir)
        {
            // Show it off-screen: hidden windows don't paint their contents
            StartPosition = FormStartPosition.Manual;
            Location = new Point(-5000, -5000);
            ShowInTaskbar = false;
            Show();
            Application.DoEvents();
            Action<string> snap = name =>
            {
                Application.DoEvents();
                using (var bmp = new Bitmap(Width, Height))
                {
                    DrawToBitmap(bmp, new Rectangle(0, 0, Width, Height));
                    bmp.Save(System.IO.Path.Combine(dir, name + ".png"));
                }
            };
            Start(); snap("1-welcome");
            NeedsClose(true); snap("2-needs-close");
            Page("Installing", null, null);
            body.Controls.Add(new Hairline { Location = new Point(0, Ui.S(64)), Width = body.Width, Value = 0.6 });
            body.Controls.Add(new Label { Text = "Applying the theme's settings", AutoSize = true, ForeColor = Palette.Muted, BackColor = Palette.Frame, Font = Ui.Body(12f), Location = new Point(0, Ui.S(78)) });
            snap("3-working");
            Finished(true); snap("4-installed");
            Page("Minimal is installed",
                 "Reinstall to refresh the theme, for example after a Firefox update. Or remove it to put Firefox back exactly as it was.",
                 Details(), Btn("Reinstall", true, () => { }), Btn("Remove", false, () => { }));
            snap("5-already-installed");
            ConfirmRemove(); snap("6-confirm-remove");
            Finished(false); snap("7-removed");
            Failed("Firefox didn't close. If it's asking whether to close several tabs, confirm it and try again."); snap("8-error");
        }

        void Work(bool install, bool closeFirst)
        {
            Page(install ? "Installing" : "Removing", null, null);
            var status = new Label { AutoSize = true, ForeColor = Palette.Muted, BackColor = Palette.Frame, Font = Ui.Body(12f), Location = new Point(0, Ui.S(78)) };
            var line = new Hairline { Location = new Point(0, Ui.S(64)), Width = body.Width };
            body.Controls.Add(status); body.Controls.Add(line);

            Action<string, double> progress = (text, v) => BeginInvoke((Action)(() => { status.Text = text; line.Value = v; }));
            new Thread(() =>
            {
                string error = null;
                try
                {
                    if (closeFirst)
                    {
                        progress("Closing Firefox", 0.05);
                        if (!Engine.CloseFirefox()) throw new Exception("Firefox didn't close. If it's asking whether to close several tabs, confirm it and try again.");
                        closedFirefox = true;
                    }
                    if (install) engine.Install(progress); else engine.Uninstall(progress);
                    Thread.Sleep(350);
                }
                catch (Exception ex) { error = ex.Message; }
                BeginInvoke((Action)(() => { if (error == null) Finished(install); else Failed(error); }));
            }) { IsBackground = true }.Start();
        }

        void Finished(bool install)
        {
            removedTheme = !install;
            bool reopened = false;
            if (closedFirefox && !noLaunch) { Engine.StartFirefox(); reopened = true; }

            var buttons = new List<FlatButton>();
            if (!reopened && !noLaunch && Engine.FirefoxPath() != null)
                buttons.Add(Btn("Open Firefox", true, () => { Engine.StartFirefox(); Close(); }));
            buttons.Add(Btn("Close", buttons.Count == 0, Close));

            if (install)
                Page("Minimal is installed",
                     "Hold Alt to show the menu bar.\nTouch the bottom edge of the window to bring up your bookmarks.\nClick into the empty address bar for your pinned sites.",
                     null, buttons.ToArray());
            else
                Page("Firefox is back the way it was",
                     "The theme is removed and your earlier look and settings are restored.",
                     null, buttons.ToArray());
        }

        void Failed(string message)
        {
            Page("That didn't finish",
                 message + "\n\nA backup of how Firefox looked before is kept in your Firefox profile, in the \u201Cminimal-theme-backup\u201D folder. Running this again is safe.",
                 null, Btn("Close", true, Close));
        }

        // ---------- Layout ----------

        FlatButton Btn(string text, bool primary, Action onClick)
        {
            var b = new FlatButton(text, primary);
            b.Click += (s, e) => onClick();
            return b;
        }

        void Page(string heading, string text, string[,] details, params FlatButton[] buttons)
        {
            body.SuspendLayout();
            foreach (Control c in body.Controls.Cast<Control>().ToList()) { body.Controls.Remove(c); c.Dispose(); }

            int y = Ui.S(8);
            var h = new Label { Text = heading, AutoSize = true, ForeColor = Palette.Text, BackColor = Palette.Frame, Font = Ui.Display(26f), Location = new Point(-Ui.S(2), y) };
            h.MouseDown += Drag;
            body.Controls.Add(h);
            y += Ui.S(48);

            if (text != null)
            {
                var t = new Label
                {
                    Text = text, ForeColor = Palette.Text, BackColor = Palette.Frame, Font = Ui.Body(13f),
                    Location = new Point(0, y), MaximumSize = new Size(Ui.S(440), 0), AutoSize = true
                };
                t.MouseDown += Drag;
                body.Controls.Add(t);
                y += t.PreferredHeight + Ui.S(22);
            }

            if (details != null)
            {
                for (int i = 0; i < details.GetLength(0); i++)
                {
                    var k = new Label { Text = details[i, 0], AutoSize = true, ForeColor = Palette.Muted, BackColor = Palette.Frame, Font = Ui.Body(12f), Location = new Point(0, y) };
                    var v = new Label { Text = details[i, 1], AutoSize = true, ForeColor = Palette.Text, BackColor = Palette.Frame, Font = Ui.Body(12f), Location = new Point(Ui.S(104), y) };
                    body.Controls.Add(k); body.Controls.Add(v);
                    y += Ui.S(22);
                }
            }

            // Buttons: primary at the right edge, the rest to its left
            int x = body.Width;
            FlatButton first = null;
            foreach (var b in buttons)
            {
                x -= b.Width;
                b.Location = new Point(x, body.Height - b.Height - Ui.S(4));
                body.Controls.Add(b);
                x -= Ui.S(8);
                if (b.Primary && first == null) first = b;
            }
            if (first == null && buttons.Length > 0) first = buttons[0];

            var version = new Label { Text = "Version " + Engine.Version, AutoSize = true, ForeColor = Palette.Hairline, BackColor = Palette.Frame, Font = Ui.Body(11.5f), Location = new Point(0, body.Height - Ui.S(26)) };
            body.Controls.Add(version);

            body.ResumeLayout();
            if (first != null && IsHandleCreated) BeginInvoke((Action)(() => first.Focus()));
        }
    }

    static class Program
    {
        [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        [STAThread]
        static int Main(string[] args)
        {
            var a = args.Select(x => x.ToLowerInvariant()).ToList();
            bool uninstall = a.Contains("/uninstall"), silent = a.Contains("/silent"),
                 noLaunch = a.Contains("/nolaunch"), noRegister = a.Contains("/noregister");
            int pi = a.IndexOf("/profile");
            string profileArg = pi >= 0 && pi + 1 < args.Length ? args[pi + 1] : null;

            string profile = profileArg ?? Engine.FindProfile();
            if (profile != null && !System.IO.Directory.Exists(profile)) profile = null;
            if (profile != null && !System.IO.Directory.Exists(profile)) profile = null;
            Engine engine = profile == null ? null : new Engine(profile) { Register = !noRegister };

            if (silent)
            {
                if (engine == null) return 2;
                try
                {
                    bool closed = false;
                    if (profileArg == null && Engine.FirefoxRunning()) { if (!Engine.CloseFirefox()) return 3; closed = true; }
                    Action<string, double> quiet = (t, v) => { };
                    if (uninstall) engine.Uninstall(quiet); else engine.Install(quiet);
                    if (closed && !noLaunch) Engine.StartFirefox();
                    if (uninstall && !noRegister && profileArg == null) Engine.ScheduleSelfRemoval();
                    return 0;
                }
                catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
            }

            try { SetProcessDpiAwarenessContext((IntPtr)(-2)); } catch { }   // sharp text on scaled displays
            using (var g = Graphics.FromHwnd(IntPtr.Zero)) Ui.Scale = g.DpiX / 96f;
#if RENDER
            Ui.Scale = 2f;   // preview build only: 2x screenshots for the web
#endif
#if RENDER
            Ui.Scale = 2f;   // preview build only: 2x screenshots for the web
#endif

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            int ri = a.IndexOf("/render");
            if (ri >= 0 && ri + 1 < args.Length)
            {
                try { using (var f = new SetupForm(engine, false, true, true)) f.RenderScreens(args[ri + 1]); }
                catch (Exception ex) { System.IO.File.WriteAllText(System.IO.Path.Combine(args[ri + 1], "error.txt"), ex.ToString()); return 1; }
                return 0;
            }
            Application.Run(new SetupForm(engine, uninstall, profileArg != null, noLaunch));
            return 0;
        }
    }
}
