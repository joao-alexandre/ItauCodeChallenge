namespace URLShortener.Dto
{
    public class CreateUrlRequest
    {
        public string Url { get; set; } = string.Empty;
        public DateTimeOffset? ExpiresAt { get; set; }
    }
}
