using Microsoft.EntityFrameworkCore;
using URLShortener.Models;

namespace URLShortener.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<UrlMapping> UrlMappings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UrlMapping>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.OriginalUrl)
                      .IsRequired();

                entity.Property(e => e.ShortKey)
                      .IsRequired()
                      .HasMaxLength(10);

                entity.HasIndex(e => e.ShortKey)
                      .IsUnique();

                entity.HasIndex(e => e.OriginalUrl)
                      .IsUnique(false);
            });
        }
    }
}
