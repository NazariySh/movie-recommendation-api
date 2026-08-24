using FluentAssertions;
using MovieRecommendation.Infrastructure.Services.Email;

namespace MovieRecommendation.UnitTests.Infrastructure.Services.Email;

public class EmailTemplatesTests
{
    [Theory]
    [InlineData("en", "Confirm your email")]
    [InlineData("EN", "Confirm your email")]
    [InlineData("uk", "Підтвердьте свою адресу")]
    [InlineData("UK", "Підтвердьте свою адресу")]
    [InlineData("fr", "Confirm your email")]
    [InlineData("", "Confirm your email")]
    public void Verification_Should_PickSubjectFromLanguage(string language, string expectedSubjectFragment)
    {
        var content = EmailTemplates.Verification("alice", "https://example.com/verify?x=1", language);

        content.Subject.Should().Contain(expectedSubjectFragment);
        content.HtmlBody.Should().Contain("alice");
        content.HtmlBody.Should().Contain("https://example.com/verify?x=1");
        content.TextBody.Should().Contain("https://example.com/verify?x=1");
    }

    [Theory]
    [InlineData("en", "Reset your password")]
    [InlineData("uk", "Скидання паролю")]
    public void PasswordReset_Should_PickSubjectFromLanguage(string language, string expectedSubjectFragment)
    {
        var content = EmailTemplates.PasswordReset("bob", "https://example.com/reset?x=1", language);

        content.Subject.Should().Contain(expectedSubjectFragment);
        content.HtmlBody.Should().Contain("https://example.com/reset?x=1");
        content.TextBody.Should().Contain("https://example.com/reset?x=1");
    }

    [Theory]
    [InlineData("en", "Welcome to MovieMatch")]
    [InlineData("uk", "Ласкаво просимо")]
    public void Welcome_Should_PickSubjectFromLanguage(string language, string expectedSubjectFragment)
    {
        var content = EmailTemplates.Welcome("carol", language);

        content.Subject.Should().Contain(expectedSubjectFragment);
        content.HtmlBody.Should().Contain("carol");
    }

    [Fact]
    public void Verification_Should_HtmlEncodeUsernameAndLink()
    {
        var content = EmailTemplates.Verification("<script>alert(1)</script>", "https://example.com/?a=b&c=<x>", "en");

        content.HtmlBody.Should().NotContain("<script>alert(1)</script>");
        content.HtmlBody.Should().Contain("&lt;script&gt;");
        content.HtmlBody.Should().NotContain("?a=b&c=<x>");
        content.HtmlBody.Should().Contain("&amp;c=&lt;x&gt;");
    }

    [Fact]
    public void Welcome_Should_NotEmitCtaSection()
    {
        var content = EmailTemplates.Welcome("dave", "en");

        content.HtmlBody.Should().NotContain("<a href=");
    }
}
