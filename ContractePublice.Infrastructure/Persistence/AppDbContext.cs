using ContractePublice.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractePublice.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ContractingAuthority> ContractingAuthorities => Set<ContractingAuthority>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<AnomalyFlag> AnomalyFlags => Set<AnomalyFlag>();
    public DbSet<DataImportLog> DataImportLogs => Set<DataImportLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ContractingAuthority>(e =>
        {
            e.HasIndex(x => x.CUI).IsUnique();
            e.HasIndex(x => x.County);
            e.Property(x => x.Name).IsRequired().HasMaxLength(500);
            e.Property(x => x.CUI).IsRequired().HasMaxLength(20);
            e.Property(x => x.County).HasMaxLength(100);
            e.Property(x => x.Type).HasMaxLength(100);
        });

        modelBuilder.Entity<Supplier>(e =>
        {
            e.HasIndex(x => x.CUI).IsUnique();
            e.HasIndex(x => x.County);
            e.Property(x => x.Name).IsRequired().HasMaxLength(500);
            e.Property(x => x.CUI).IsRequired().HasMaxLength(20);
            e.Property(x => x.County).HasMaxLength(100);
        });

        modelBuilder.Entity<Contract>(e =>
        {
            e.HasIndex(x => x.SeapId).IsUnique();
            e.HasIndex(x => x.County);
            e.HasIndex(x => x.PublishedAt);
            e.HasIndex(x => x.CpvCode);
            e.HasIndex(x => x.AwardProcedure);
            e.Property(x => x.Title).IsRequired().HasMaxLength(1000);
            e.Property(x => x.SeapId).IsRequired().HasMaxLength(50);
            e.Property(x => x.CpvCode).HasMaxLength(20);
            e.Property(x => x.CpvDescription).HasMaxLength(500);
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Property(x => x.County).HasMaxLength(100);
            e.Property(x => x.EstimatedValue).HasPrecision(18, 2);
            e.Property(x => x.AwardedValue).HasPrecision(18, 2);

            e.HasOne(x => x.ContractingAuthority)
             .WithMany(x => x.Contracts)
             .HasForeignKey(x => x.ContractingAuthorityId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Supplier)
             .WithMany(x => x.Contracts)
             .HasForeignKey(x => x.SupplierId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AnomalyFlag>(e =>
        {
            e.HasIndex(x => x.FlagType);
            e.HasIndex(x => x.Severity);
            e.Property(x => x.FlagType).IsRequired().HasMaxLength(100);
            e.Property(x => x.Severity).IsRequired().HasMaxLength(20);
            e.Property(x => x.Description).IsRequired().HasMaxLength(2000);
        });

        modelBuilder.Entity<DataImportLog>(e =>
        {
            e.HasIndex(x => x.ImportedAt);
            e.Property(x => x.Source).IsRequired().HasMaxLength(100);
            e.Property(x => x.Status).IsRequired().HasMaxLength(50);
            e.Property(x => x.Notes).HasMaxLength(2000);
        });
    }
}
