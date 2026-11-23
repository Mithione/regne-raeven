using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Regneraeven.Web.Data;

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<SavedProblem> Problems => Set<SavedProblem>();
    public DbSet<ProblemTag> ProblemTags => Set<ProblemTag>();
    public DbSet<GameTask> GameTasks => Set<GameTask>();
    public DbSet<XpEvent> XpEvents => Set<XpEvent>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Profile>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(40);
            e.Property(p => p.Avatar).HasMaxLength(16);
            e.Property(p => p.Color).HasMaxLength(16);
            e.Property(p => p.Frame).HasMaxLength(24);
            e.Property(p => p.Background).HasMaxLength(24);
            e.Property(p => p.Theme).HasMaxLength(24);
        });

        b.Entity<SavedProblem>(e =>
        {
            e.Property(p => p.Text).HasMaxLength(2000);
            e.Property(p => p.Note).HasMaxLength(2000);
            e.Property(p => p.MyAnswer).HasMaxLength(200);
            e.HasIndex(p => new { p.ProfileId, p.UpdatedAt });
            e.HasOne(p => p.Profile).WithMany().HasForeignKey(p => p.ProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Tags).WithOne().HasForeignKey(t => t.SavedProblemId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ProblemTag>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(40);
            e.HasIndex(t => t.Name);
            e.Ignore(t => t.Topic);
        });

        b.Entity<GameTask>(e =>
        {
            e.HasIndex(t => new { t.ProfileId, t.Day, t.IsDaily });
            e.HasOne<Profile>().WithMany().HasForeignKey(t => t.ProfileId).OnDelete(DeleteBehavior.Cascade);
            e.Property(t => t.LastAnswer).HasMaxLength(100);
        });

        b.Entity<XpEvent>(e =>
        {
            e.HasIndex(x => new { x.ProfileId, x.At });
            e.HasOne<Profile>().WithMany().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Reason).HasMaxLength(200);
        });
    }
}

/// <summary>Used by "dotnet ef migrations add" only.</summary>
public class AppDbDesignTimeFactory : IDesignTimeDbContextFactory<AppDb>
{
    public AppDb CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDb>().UseSqlite("Data Source=design.db").Options);
}
