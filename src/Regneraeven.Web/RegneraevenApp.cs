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
    /// <summary>Full path of the SQLite database file.</summary>
    public required string DatabasePath { get; init; }
}

/// <summary>
/// Plugs Regneræven into any ASP.NET Core host. The Windows program uses it today;
/// a website later only needs its own host project (plus login) that calls the same two methods.
/// </summary>
public static class RegneraevenApp
{
    public static IServiceCollection AddRegneraeven(this IServiceCollection services, RegneraevenOptions options)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(options.DatabasePath)!);
        services.AddSingleton(options);
        services.AddDbContext<AppDb>(o => o.UseSqlite($"Data Source={options.DatabasePath}"));
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

    /// <summary>Creates/updates the database and maps the pages. Call after any host-specific middleware.</summary>
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
        app.MapControllers();
        return app;
    }
}
