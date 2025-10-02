using FluentValidation;
using URLShortener.Dto;

namespace URLShortener.Validators
{
    public class CreateUrlRequestValidator : AbstractValidator<CreateUrlRequest>
    {
        public CreateUrlRequestValidator()
        {
            RuleFor(x => x.Url)
                .NotEmpty().WithMessage("Url is required.")
                .Must(IsValidHttpUrl).WithMessage("Invalid URL. Use HTTP or HTTPS.");

            RuleFor(x => x.ExpiresAt)
                .GreaterThan(DateTimeOffset.UtcNow)
                .When(x => x.ExpiresAt.HasValue)
                .WithMessage("ExpiresAt must be a future date/time.");
        }

        private bool IsValidHttpUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }
    }
}
