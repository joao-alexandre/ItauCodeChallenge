using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URLShortener.Data;
using URLShortener.Services;
using Prometheus;

namespace URLShortener.Controllers
{
    [ApiController]
    [Route("")]
    public class RedirectController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly ICacheService _cache;

        public RedirectController(AppDbContext context, ICacheService cache)
        {
            _context = context;
            _cache = cache;
        }

        [HttpGet("{shortKey}")]
        public async Task<IActionResult> RedirectToOriginalUrl(string shortKey)
        {
            var UrlHits = Metrics.CreateCounter("url_hits_total", "Total de hits por URL", new CounterConfiguration
            {
                LabelNames = new[] { "shortKey" }
            });
            
            if (string.IsNullOrWhiteSpace(shortKey))
                return BadRequest(new { message = "Short key is required." });  

            var url = await _context.UrlMappings.FirstOrDefaultAsync(u => u.ShortKey == shortKey);

            if (url == null)
                return NotFound(new { message = "URL not found." });

            url.Hits++;
            await _context.SaveChangesAsync();

            if (_cache != null)
            {
                await _cache.SetAsync(url.ShortKey, url, TimeSpan.FromMinutes(60));
            }

            UrlHits.WithLabels(shortKey).Inc();

            return Redirect(url.OriginalUrl);
        }
    }
}
