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
    public DbSet<FSCurrency> FSCurrencies { get; set; }
    public DbSet<FSBudget> FSBudgets { get; set; }
    public DbSet<FSBudgetPeriod> FSBudgetPeriods { get; set; }
    public DbSet<FSBudgetCategory> FSBudgetCategories { get; set; }
    public DbSet<FSFile> FSFiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<FSCurrency>()
        .ToTable("FSCurrencies");

        modelBuilder.Entity<FSCategory>()
            .Property(c => c.Type)
            .HasConversion<string>();

        modelBuilder.Entity<FSCategory>()
            .HasOne<FSUser>()
            .WithMany()
            .HasForeignKey(t => t.FSUserId)
            .OnDelete(DeleteBehavior.Restrict);
        

        modelBuilder.Entity<FSBudget>(entity=>
        {
            entity
                .HasOne<FSUser>()
                .WithMany()
                .HasForeignKey(b => b.FSUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
               .HasOne<FSCurrency>()
               .WithMany()
               .HasForeignKey(b => b.FSCurrencyCode)
               .OnDelete(DeleteBehavior.Restrict);

            entity.Property(b => b.Frequency).HasConversion<string>();
        });

        modelBuilder.Entity<FSBudgetCategory>().HasKey(bc => new { bc.BudgetId, bc.CategoryId });

        modelBuilder.Entity<FSBudgetCategory>(entity=>
        {
                entity
                .HasOne(bc => bc.Budget)
                .WithMany(b => b.BudgetCategories)
                .HasForeignKey(bc => bc.BudgetId)
                .OnDelete(DeleteBehavior.Restrict);

              entity
                .HasOne(bc => bc.Category)
                .WithMany(c => c.BudgetCategories)
                .HasForeignKey(bc => bc.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FSBudgetPeriod>()
                .HasOne(p => p.Budget)
                .WithMany(b => b.Periods)
                .HasForeignKey(p => p.BudgetId)
                .OnDelete(DeleteBehavior.Cascade);
        



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
              .OnDelete(DeleteBehavior.Restrict);

            entity
             .HasOne<FSCurrency>()
             .WithMany()
             .HasForeignKey(fx => fx.To)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FSExchangeRate>()
       .HasIndex(e => new { e.From, e.To, e.Date })
       .IsUnique();

        modelBuilder.Entity<FSFile>(entity =>
        {
            entity
              .HasOne<FSUser>()
              .WithMany()
              .HasForeignKey(f => f.FSUserId)
              .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.Transaction)
                .WithMany(t => t.Files)
                .HasForeignKey(f => f.FSTransactionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

    }
}