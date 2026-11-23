using System.Windows.Forms;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Regneraeven.Web;

namespace Regneraeven.Desktop;

public static class Program
{
    /// <summary>
    /// Regneræven as a Windows program: the web app runs on 127.0.0.1 (random port) and is shown in
    /// a WebView2 window. Start with --browser to run only the server (development / testing in a browser).
    /// </summary>
    [STAThread]
    public static int Main(string[] args)
    {
        var browserMode = args.Contains("--browser", StringComparer.OrdinalIgnoreCase);

        if (!browserMode)
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        }

        // One window per Windows user (browser/dev mode is not counted).
        var isFirst = true;
        using var singleInstance = browserMode ? null : new Mutex(true, $"Regneraeven-{Environment.UserName}", out isFirst);
        if (!isFirst)
        {
            MessageBox.Show("Regneræven kører allerede.", "Regneræven", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 0;
        }

        WebApplication app;
        try
        {
            app = BuildApp(args, browserMode);
        }
        catch (Exception ex)
        {
            if (browserMode) throw;
            MessageBox.Show("Regneræven kunne ikke starte:\n\n" + ex.Message, "Regneræven", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }

        var access = app.Services.GetRequiredService<LocalAccess>();
        if (browserMode)
        {
            app.Lifetime.ApplicationStarted.Register(() =>
            {
                foreach (var url in app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses)
                    app.Logger.LogWarning("Regneræven kører. Åbn {Url}/?k={Token}", url, access.Token);
            });
            app.Run();
            return 0;
        }

        app.StartAsync().GetAwaiter().GetResult();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        Application.Run(new MainForm(new Uri($"{address}/?k={access.Token}")));
        app.StopAsync().GetAwaiter().GetResult();
        return 0;
    }

    /// <summary>%APPDATA%\Regneraeven – or the folder in REGNERAEVEN_DATA (used by the dev profiles).</summary>
    public static string DataFolder
    {
        get
        {
            var custom = Environment.GetEnvironmentVariable("REGNERAEVEN_DATA");
            if (!string.IsNullOrWhiteSpace(custom))
                return Path.GetFullPath(Path.IsPathRooted(custom) ? custom : Path.Combine(Directory.GetCurrentDirectory(), custom));
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Regneraeven");
        }
    }

    private static WebApplication BuildApp(string[] args, bool browserMode)
    {
        // Content lives next to the exe – not in whatever folder a shortcut starts in.
        // In a development build wwwroot is not copied; the static web assets manifest points at the projects instead.
        var baseDir = AppContext.BaseDirectory;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args.Where(a => !a.Equals("--browser", StringComparison.OrdinalIgnoreCase)).ToArray(),
            ContentRootPath = baseDir,
        });
        if (!Directory.Exists(Path.Combine(baseDir, "wwwroot"))) builder.WebHost.UseStaticWebAssets();

        if (!browserMode)
            builder.WebHost.UseUrls("http://127.0.0.1:0");
        else if (string.IsNullOrEmpty(builder.Configuration["urls"]) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
            builder.WebHost.UseUrls("http://localhost:5187");

        builder.Services.AddSingleton<LocalAccess>();
        builder.Services.AddRegneraeven(new RegneraevenOptions { DatabasePath = Path.Combine(DataFolder, "regneraeven.db") });

        var app = builder.Build();
        app.UseMiddleware<LocalAccessMiddleware>();
        app.UseRegneraeven();
        return app;
    }
}
