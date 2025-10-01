using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URLShortener.Data;

namespace URLShortener.Controllers
{
    [ApiController]
    [Route("")]
    public class RedirectController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RedirectController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{shortKey}")]
        public async Task<IActionResult> RedirectToOriginalUrl(string shortKey)
        {
            var url = await _context.UrlMappings.FirstOrDefaultAsync(u => u.ShortKey == shortKey);

            if (url == null)
                return NotFound(new { message = "URL not found." });

            url.Hits++;
            await _context.SaveChangesAsync();

            return Redirect(url.OriginalUrl);
        }
    }
}
