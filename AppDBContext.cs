using Finsight.Models;
using Finsight.Services;
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

        modelBuilder.Entity<FSCategory>()
            .HasOne<FSUser>()
            .WithMany()
            .HasForeignKey(t => t.FSUserId)
            .OnDelete(DeleteBehavior.Restrict);


        modelBuilder.Entity<FSTransaction>(entity =>
        {
            entity
                .HasOne<FSUser>()
                .WithMany()
                .HasForeignKey(t => t.FSUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne<FSCategory>()
                .WithMany()
                .HasForeignKey(t => t.FSCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne<FSSubCategory>()
                .WithMany()
                .HasForeignKey(t => t.FSSubCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
               .HasOne<FSCurrency>()
               .WithMany()
               .HasForeignKey(t => t.FSCurrencyCode)
               .OnDelete(DeleteBehavior.Restrict);

            entity.Property(t => t.Type).HasConversion<string>();
            entity.Property(t => t.SubType).HasConversion<string>();
            entity.Property(t => t.Mode).HasConversion<string>();
        });

        modelBuilder.Entity<FSUser>()
               .HasIndex(u => u.Email)
               .IsUnique();
    }
}