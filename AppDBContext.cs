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
    public DbSet<FSExchangeRate> FSExchangeRates { get; set; }

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

        modelBuilder.Entity<FSUser>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity
              .HasOne<FSCurrency>()
              .WithMany()
              .HasForeignKey(u => u.DefaultCurrency)
              .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FSExchangeRate>(entity =>
        {
            entity
              .HasOne<FSCurrency>()
              .WithMany()
              .HasForeignKey(fx => fx.From)
              .HasConstraintName("FK_FSExchangeRate_FromCurrency")
              .OnDelete(DeleteBehavior.Restrict);

            entity
             .HasOne<FSCurrency>()
             .WithMany()
             .HasForeignKey(fx => fx.To)
             .HasConstraintName("FK_FSExchangeRate_ToCurrency")
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FSExchangeRate>()
       .HasIndex(e => new { e.From, e.To, e.Date })
       .IsUnique();

    }
}