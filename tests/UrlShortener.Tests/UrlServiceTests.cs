using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using URLShortener.Data;
using URLShortener.Services;
using Xunit;

namespace UrlShortener.Tests
{
    public class UrlServiceTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private AppDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateUrlMapping()
        {
            var db = CreateInMemoryDb();
            var service = new UrlService(db, NullLogger<UrlService>.Instance);

            var originalUrl = "https://example.com";

            var mapping = await service.CreateAsync(originalUrl);

            Assert.NotNull(mapping);
            Assert.Equal(originalUrl, mapping.OriginalUrl);
            Assert.False(string.IsNullOrEmpty(mapping.ShortKey));
            Assert.Equal(0, mapping.Hits);
        }

        [Fact]
        public async Task GetByShortKeyAsync_NonExistent_ReturnsNull()
        {
            var db = CreateInMemoryDb();
            var service = new UrlService(db, NullLogger<UrlService>.Instance);

            var result = await service.GetByShortKeyAsync("nonexistent");
            Assert.Null(result);
        }


        [Fact]
        public async Task GetByShortKeyAsync_ShouldReturnMapping()
        {
            var db = GetDbContext();
            var service = new UrlService(db, NullLogger<UrlService>.Instance);

            var mapping = await service.CreateAsync("https://globo.com");
            var result = await service.GetByShortKeyAsync(mapping.ShortKey);

            Assert.NotNull(result);
            Assert.Equal(mapping.ShortKey, result!.ShortKey);
        }

        [Fact]
        public async Task DeleteByShortKeyAsync_RemovesMapping()
        {
            var db = CreateInMemoryDb();
            var service = new UrlService(db, NullLogger<UrlService>.Instance);

            var mapping = await service.CreateAsync("https://example.net");
            var removed = await service.DeleteByShortKeyAsync(mapping.ShortKey);

            Assert.True(removed);

            var fetch = await service.GetByShortKeyAsync(mapping.ShortKey);
            Assert.Null(fetch);
        }


        [Fact]
        public async Task IncrementHitsAsync_ShouldIncreaseHits()
        {
            var db = GetDbContext();
            var service = new UrlService(db, NullLogger<UrlService>.Instance);

            var mapping = await service.CreateAsync("https://globo.com");
            await service.IncrementHitsAsync(mapping.ShortKey);
            await service.IncrementHitsAsync(mapping.ShortKey);

            var updated = await service.GetByShortKeyAsync(mapping.ShortKey);
            Assert.Equal(2, updated!.Hits);
        }
    }
}
