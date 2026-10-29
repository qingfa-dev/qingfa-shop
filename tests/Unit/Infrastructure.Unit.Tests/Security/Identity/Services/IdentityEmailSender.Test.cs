using Application.Common.Security.Authentication.Identity;

using Infrastructure.Security.Identity.Services;

using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Unit.Tests.Security.Identity.Services;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Email")]
public sealed class IdentityEmailSenderTest
{
    [Fact(DisplayName = "Logging email sender should accept confirmation and reset messages")]
    public async Task Sender_ShouldAcceptMessages()
    {
        var sender = new LoggingIdentityEmailSender(NullLogger<LoggingIdentityEmailSender>.Instance);

        (await sender.SendConfirmationAsync("alice@example.com", "https://confirm")).IsSuccess
            .Should().BeTrue();
        (await sender.SendPasswordResetAsync("alice@example.com", "https://reset")).IsSuccess
            .Should().BeTrue();
    }
}
