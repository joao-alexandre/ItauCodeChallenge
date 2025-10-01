using Microsoft.AspNetCore.Mvc;
using URLShortener.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/urls")]
public class UrlsController : ControllerBase
{
    private readonly IUrlService _service;
    private readonly ILogger<UrlsController> _logger;

    public UrlsController(IUrlService service, ILogger<UrlsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    public record CreateRequest(string Url, DateTimeOffset? ExpiresAt);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Url) || !Uri.TryCreate(req.Url, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            return BadRequest(new { error = "Invalid URL. Use http or https." });

        try
        {
            var mapping = await _service.CreateAsync(req.Url, req.ExpiresAt, ct);
            return CreatedAtAction(nameof(Get), new { shortKey = mapping.ShortKey }, new
            {
                shortKey = mapping.ShortKey,
                originalUrl = mapping.OriginalUrl,
                createdAt = mapping.CreatedAt,
                expiresAt = mapping.ExpiresAt,
                hits = mapping.Hits
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating short URL");
            return StatusCode(500, new { error = "Internal error" });
        }
    }

    [HttpGet("{shortKey}")]
    public async Task<IActionResult> Get(string shortKey, CancellationToken ct)
    {
        var mapping = await _service.GetByShortKeyAsync(shortKey, ct);
        if (mapping == null) return NotFound(new { error = "Not found" });
        return Ok(new
        {
            shortKey = mapping.ShortKey,
            originalUrl = mapping.OriginalUrl,
            createdAt = mapping.CreatedAt,
            updatedAt = mapping.UpdatedAt,
            expiresAt = mapping.ExpiresAt,
            hits = mapping.Hits
        });
    }

    [HttpDelete("{shortKey}")]
    public async Task<IActionResult> Delete(string shortKey, CancellationToken ct)
    {
        var removed = await _service.DeleteByShortKeyAsync(shortKey, ct);
        if (!removed) return NotFound(new { error = "Not found" });
        return NoContent();
    }
}
