using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace URLShortener.Models
{
    [Table("url_mappings")]
    public class UrlMapping
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(64)]
        [JsonPropertyName("shortKey")]
        public string ShortKey { get; set; } = null!;

        [Required]
        [JsonPropertyName("originalUrl")]
        public string OriginalUrl { get; set; } = null!;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ExpiresAt { get; set; }
        public int Hits { get; set; } = 0;
    }
}
