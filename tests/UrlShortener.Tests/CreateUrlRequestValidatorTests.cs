using FluentValidation.TestHelper;
using URLShortener.Dto;
using URLShortener.Validators;
using Xunit;

public class CreateUrlRequestValidatorTests
{
    private readonly CreateUrlRequestValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("notaurl")]
    [InlineData("ftp://example.com")]
    public void Url_Invalid_ReturnsError(string url)
    {
        var model = new CreateUrlRequest { Url = url };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Url);
    }

    [Fact]
    public void ExpiresAt_Past_ReturnsError()
    {
        var model = new CreateUrlRequest { Url = "https://example.com", ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1) };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ExpiresAt);
    }

    [Fact]
    public void ValidModel_Passes()
    {
        var model = new CreateUrlRequest { Url = "https://example.com", ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
