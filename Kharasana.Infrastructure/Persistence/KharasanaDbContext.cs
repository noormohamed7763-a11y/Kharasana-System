using Kharasana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Infrastructure.Persistence;

public class KharasanaDbContext : DbContext
{
    public KharasanaDbContext(DbContextOptions<KharasanaDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Factory> Factories { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<ConcreteType> ConcreteTypes { get; set; }

    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all IEntityTypeConfiguration<T> classes automatically
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KharasanaDbContext).Assembly);

        // ✅ Global Query Filters — الحذف الناعم لجميع الكيانات
        //    أي استعلام يستبعد الكيانات المحذوفة تلقائياً
        modelBuilder.Entity<Factory>()
            .HasQueryFilter(f => !f.IsDeleted);

        modelBuilder.Entity<User>()
            .HasQueryFilter(u => !u.IsDeleted);

        modelBuilder.Entity<Order>()
            .HasQueryFilter(o => !o.IsDeleted);

        modelBuilder.Entity<ConcreteType>()
            .HasQueryFilter(c => !c.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }
}