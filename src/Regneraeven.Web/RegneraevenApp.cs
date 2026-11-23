using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Regneraeven.Web.Data;
using Regneraeven.Web.Services;

namespace Regneraeven.Web;

public sealed class RegneraevenOptions
{
    /// <summary>Chooses the database: SQLite for the Windows program, SQL Server for the website.</summary>
    public required Action<DbContextOptionsBuilder> ConfigureDatabase { get; init; }

    /// <summary>
    /// Website mode: every profile belongs to the logged-in parent account (the user id claim),
    /// and a parent only sees their own children. Off in the Windows program.
    /// </summary>
    public bool ProfilesPerAccount { get; init; }

    /// <summary>When set, the menu shows a "Log ud" button that posts here.</summary>
    public string? LogoutPath { get; init; }

    /// <summary>The Windows program: a SQLite file on this computer.</summary>
    public static RegneraevenOptions Sqlite(string databasePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        return new RegneraevenOptions { ConfigureDatabase = o => o.UseSqlite($"Data Source={databasePath}") };
    }
}

/// <summary>
/// Plugs Regneræven into any ASP.NET Core host. The Windows program uses it today;
/// a website later only needs its own host project (plus login) that calls the same two methods.
/// </summary>
public static class RegneraevenApp
{
    public static IServiceCollection AddRegneraeven(this IServiceCollection services, RegneraevenOptions options)
    {
        services.AddSingleton(options);
        services.AddDbContext<AppDb>(options.ConfigureDatabase);
        services.AddHttpContextAccessor();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CurrentProfile>();
        services.AddScoped<GameService>();
        services.AddScoped<RequireProfileFilter>();

        services.AddAntiforgery(o =>
        {
            o.HeaderName = "RequestVerificationToken";
            o.Cookie.Name = "rr.af";
        });
        services.AddControllersWithViews(o =>
            {
                o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                o.Filters.AddService<RequireProfileFilter>();
                o.Filters.Add<UserFacingExceptionFilter>();
            })
            .AddApplicationPart(typeof(RegneraevenApp).Assembly);
        return services;
    }

    /// <summary>Creates/updates the database and maps the pages. Call after any host-specific middleware (login etc.).</summary>
    public static WebApplication UseRegneraeven(this WebApplication app)
    {
        var da = CultureInfo.GetCultureInfo("da-DK");
        CultureInfo.DefaultThreadCurrentCulture = da;
        CultureInfo.DefaultThreadCurrentUICulture = da;

        using (var scope = app.Services.CreateScope())
            scope.ServiceProvider.GetRequiredService<AppDb>().Database.Migrate();

        if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/fejl");
        app.UseStaticFiles();
        app.UseRouting();
        if (app.Services.GetRequiredService<RegneraevenOptions>().ProfilesPerAccount)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }
        app.MapControllers();
        return app;
    }
}
