using System;
using Microsoft.EntityFrameworkCore;
using ConcreteMap.Domain.Entities;

namespace ConcreteMap.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Factory> Factories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasPostgresExtension("pg_trgm");
            modelBuilder.Entity<Factory>()
                .HasMany(f => f.Products)
                .WithOne(p => p.Factory)
                .HasForeignKey(p => p.FactoryId);
            modelBuilder.Entity<Factory>().HasIndex(f => f.Name).HasMethod("gin").HasOperators("gin_trgm_ops");
            modelBuilder.Entity<Factory>().HasIndex(f => f.ProductCategories).HasMethod("gin").HasOperators("gin_trgm_ops");
            modelBuilder.Entity<Factory>().HasIndex(f => f.Comment).HasMethod("gin").HasOperators("gin_trgm_ops");
            modelBuilder.Entity<Factory>().HasIndex(f => f.PriceListContent).HasMethod("gin").HasOperators("gin_trgm_ops");
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Name)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
        }
    }
}