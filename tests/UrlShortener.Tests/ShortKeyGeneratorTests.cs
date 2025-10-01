using URLShortener.Utils;
using Xunit;

namespace UrlShortener.Tests
{
    public class ShortKeyGeneratorTests
    {
        [Fact]
        public void Generate_ReturnsUniqueKey()
        {
            var key1 = ShortKeyGenerator.Generate();
            var key2 = ShortKeyGenerator.Generate();

            Assert.False(string.IsNullOrWhiteSpace(key1));
            Assert.False(string.IsNullOrWhiteSpace(key2));
            Assert.NotEqual(key1, key2);
        }
    }
}
