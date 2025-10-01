using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace URLShortener.Models
{
    [Table("url_mappings")] // garante que bate com a tabela do Postgres
    public class UrlMapping
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(64)]
        public string ShortKey { get; set; } = null!;

        [Required]
        public string OriginalUrl { get; set; } = null!;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ExpiresAt { get; set; }
        public int Hits { get; set; } = 0;
    }
}
