using System.Diagnostics;
using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Regneraeven.Desktop;

/// <summary>The application window: a WebView2 browser showing Regneræven served from 127.0.0.1.</summary>
public sealed class MainForm : Form
{
    private sealed record SavedWindow(int X, int Y, int Width, int Height, bool Maximized);

    private readonly WebView2 _web;
    private readonly Uri _start;
    private static string StateFile => Path.Combine(Program.DataFolder, "window.json");

    public MainForm(Uri start)
    {
        _start = start;
        Text = "Regneræven";
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        MinimumSize = new Size(900, 620);
        StartPosition = FormStartPosition.Manual;
        RestoreSavedBounds();
        BackColor = ColorTranslator.FromHtml("#fffaf4");

        _web = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = BackColor };
        Controls.Add(_web);

        Load += async (_, _) => await InitAsync();
        FormClosing += (_, _) => SaveBounds();
    }

    private void RestoreSavedBounds()
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1366, 860);
        try
        {
            if (File.Exists(StateFile) && JsonSerializer.Deserialize<SavedWindow>(File.ReadAllText(StateFile)) is { } w
                && w.Width >= MinimumSize.Width && w.Height >= MinimumSize.Height)
            {
                var rect = new Rectangle(w.X, w.Y, w.Width, w.Height);
                if (Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(rect)))
                {
                    Bounds = rect;
                    if (w.Maximized) WindowState = FormWindowState.Maximized;
                    return;
                }
            }
        }
        catch { /* fall back to defaults */ }
        var width = Math.Min(1366, area.Width - 40);
        var height = Math.Min(900, area.Height - 40);
        Bounds = new Rectangle(area.Left + (area.Width - width) / 2, area.Top + (area.Height - height) / 2, width, height);
    }

    private void SaveBounds()
    {
        try
        {
            var b = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            Directory.CreateDirectory(Program.DataFolder);
            File.WriteAllText(StateFile, JsonSerializer.Serialize(new SavedWindow(b.X, b.Y, b.Width, b.Height, WindowState == FormWindowState.Maximized)));
        }
        catch { }
    }

    private async Task InitAsync()
    {
        try
        {
            var options = new CoreWebView2EnvironmentOptions { Language = "da-DK" };
            var env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(Program.DataFolder, "WebView2"), options);
            await _web.EnsureCoreWebView2Async(env);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            MessageBox.Show(this, "Microsoft Edge WebView2 Runtime mangler. Installér den fra https://go.microsoft.com/fwlink/p/?LinkId=2124703 og start Regneræven igen.",
                "Regneræven", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
            return;
        }

        var core = _web.CoreWebView2;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.AreDevToolsEnabled = Debugger.IsAttached;
        core.Settings.IsPasswordAutosaveEnabled = false;
        core.Settings.IsGeneralAutofillEnabled = false;

        // Links to other sites open in the normal browser; Regneræven's own pages stay in the window.
        core.NewWindowRequested += (_, e) =>
        {
            e.Handled = true;
            if (IsLocal(e.Uri)) core.Navigate(e.Uri);
            else OpenExternal(e.Uri);
        };
        core.NavigationStarting += (_, e) =>
        {
            if (IsLocal(e.Uri) || e.Uri.StartsWith("about:", StringComparison.OrdinalIgnoreCase)) return;
            e.Cancel = true;
            OpenExternal(e.Uri);
        };
        core.DocumentTitleChanged += (_, _) => Text = string.IsNullOrWhiteSpace(core.DocumentTitle) ? "Regneræven" : core.DocumentTitle;

        core.Navigate(_start.AbsoluteUri);
    }

    private bool IsLocal(string uri) =>
        Uri.TryCreate(uri, UriKind.Absolute, out var u) && u.Host == _start.Host && u.Port == _start.Port;

    private static void OpenExternal(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeHttp))
            Process.Start(new ProcessStartInfo(u.AbsoluteUri) { UseShellExecute = true });
    }
}
