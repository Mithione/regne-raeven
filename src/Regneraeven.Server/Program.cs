using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Regneraeven.Server.Auth;
using Regneraeven.Web;

// Regneræven as a website. Same app as the Windows program (Regneraeven.Web), plus:
//  - parent accounts (ASP.NET Identity) – every child profile belongs to a parent
//  - SQL Server / Azure SQL instead of a local SQLite file
//  - settings for Azure Container Apps (HTTPS proxy, health check, keys that survive restarts)

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Regneraeven");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Mangler forbindelsesstrengen ConnectionStrings:Regneraeven (miljøvariabel ConnectionStrings__Regneraeven).");

// Azure SQL (serverless/free) pauses when idle; the first query after a pause fails briefly, so retry.
void SqlServer(DbContextOptionsBuilder o) => o.UseSqlServer(connectionString, sql =>
{
    sql.MigrationsAssembly(typeof(AuthDb).Assembly.FullName);
    sql.EnableRetryOnFailure(maxRetryCount: 6, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
});

builder.Services.AddDbContext<AuthDb>(SqlServer);
builder.Services.AddIdentityCore<IdentityUser>(o =>
    {
        o.User.RequireUniqueEmail = true;
        o.Password.RequiredLength = 8;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireUppercase = false;
        o.Lockout.MaxFailedAccessAttempts = 8;
    })
    .AddEntityFrameworkStores<AuthDb>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/konto/log-ind";
    o.AccessDeniedPath = "/konto/log-ind";
    o.Cookie.Name = "rr.auth";
    o.ExpireTimeSpan = TimeSpan.FromDays(30);
    o.SlidingExpiration = true;
});
// Everything needs a logged-in parent, except pages marked [AllowAnonymous] (login, sign-up, health check).
builder.Services.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Keys for login cookies are kept in the database – otherwise everyone is logged out when the container restarts.
builder.Services.AddDataProtection().PersistKeysToDbContext<AuthDb>().SetApplicationName("Regneraeven");

// Container Apps terminates HTTPS in front of the app.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

builder.Services.AddHealthChecks();
builder.Services.AddRegneraeven(new RegneraevenOptions
{
    ConfigureDatabase = SqlServer,
    ProfilesPerAccount = true,
    LogoutPath = "/konto/log-ud",
});

var app = builder.Build();
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment()) app.UseHsts();

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<AuthDb>().Database.MigrateAsync();

app.UseRegneraeven();
app.MapHealthChecks("/healthz").AllowAnonymous();
app.Run();

public partial class Program;
