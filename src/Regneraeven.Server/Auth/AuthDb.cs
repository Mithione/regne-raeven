using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Regneraeven.Web.Data;

namespace Regneraeven.Server.Auth;

/// <summary>Parent accounts and the cookie keys. Lives in the same database as the app data, in its own tables.</summary>
public class AuthDb(DbContextOptions<AuthDb> options) : IdentityDbContext<IdentityUser>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
}

/// <summary>
/// Used by "dotnet ef" only: SQL Server migrations for the website live in this project
/// (the SQLite migrations for the Windows program live in Regneraeven.Web).
/// </summary>
public class AppDbSqlServerDesignFactory : IDesignTimeDbContextFactory<AppDb>
{
    public AppDb CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<AppDb>()
        .UseSqlServer(DesignConnection, sql => sql.MigrationsAssembly(typeof(AuthDb).Assembly.FullName)).Options);

    internal const string DesignConnection = "Server=(localdb)\\MSSQLLocalDB;Database=RegneraevenDesign;Trusted_Connection=True;TrustServerCertificate=True";
}

public class AuthDbDesignFactory : IDesignTimeDbContextFactory<AuthDb>
{
    public AuthDb CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<AuthDb>()
        .UseSqlServer(AppDbSqlServerDesignFactory.DesignConnection).Options);
}
