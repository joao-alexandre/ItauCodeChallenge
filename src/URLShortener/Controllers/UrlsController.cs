using Microsoft.AspNetCore.Mvc;
using URLShortener.Dto;
using URLShortener.Services;
using Prometheus;

namespace UrlShortener.Api.Controllers
{
    [ApiController]
    [Route("api/urls")]
    public class UrlsController : ControllerBase
    {
        private readonly IUrlService _service;
        private readonly ILogger<UrlsController> _logger;

        private static readonly Counter UrlOperations = Metrics.CreateCounter(
            "url_operations_total",
            "Create and Delete Operations Total",
            new CounterConfiguration
            {
                LabelNames = new[] { "operation" }
            });

        public UrlsController(IUrlService service, ILogger<UrlsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUrlRequest req, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var mapping = await _service.CreateAsync(req.Url, req.ExpiresAt, ct);

                UrlOperations.WithLabels("created").Inc();

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
                UrlOperations.WithLabels("error").Inc();
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

            if (removed)
            {
                UrlOperations.WithLabels("deleted").Inc();
                return NoContent();
            }

            return NotFound(new { error = "Not found" });
        }
    }
}
