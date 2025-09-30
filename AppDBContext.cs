using Finsight.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : IdentityDbContext<FSUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<FSCategory> Categories { get; set; }
    public DbSet<FSSubCategory> SubCategories { get; set; }

    public DbSet<FSTransaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FSCategory>()
            .Property(c => c.Type)
            .HasConversion<string>();

        modelBuilder.Entity<FSTransaction>()
                 .Property(t => t.Type)
                 .HasConversion<string>();

        modelBuilder.Entity<FSTransaction>()
            .Property(t => t.SubType)
            .HasConversion<string>();

        modelBuilder.Entity<FSTransaction>()
            .Property(t => t.Mode)
            .HasConversion<string>();

        modelBuilder.Entity<FSTransaction>()
            .Property(t => t.Currency)
            .HasConversion<string>();

        modelBuilder.Entity<FSUser>()
               .HasIndex(u => u.Email)
               .IsUnique();
    }
}