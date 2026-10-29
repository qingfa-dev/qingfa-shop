using FluentEmail.Core;
using FluentEmail.Core.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Application.Common.Integrations.Notifications.Templates;

namespace Infrastructure.Unit.Tests.Integrations.Notifications.Channels.Email.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class SmtpProviderBehaviorTests
{
    private static Mock<IFluentEmail> CreateBaseEmailMock()
    {
        Mock<IFluentEmail> mock = new();
        mock.Setup(x => x.SetFrom(It.IsAny<string>(), It.IsAny<string>())).Returns(mock.Object);
        mock.Setup(x => x.To(It.IsAny<string>(), It.IsAny<string>())).Returns(mock.Object);
        mock.Setup(x => x.Subject(It.IsAny<string>())).Returns(mock.Object);
        mock.Setup(x => x.PlaintextAlternativeBody(It.IsAny<string>())).Returns(mock.Object);
        mock.Setup(x => x.Body(It.IsAny<string>(), It.IsAny<bool>())).Returns(mock.Object);
        mock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SendResponse());
        return mock;
    }
    private static SmtpProvider CreateProvider(
        Mock<IFluentEmail>? fluentEmailMock = null,
        SmtpProviderSetting? setting = null,
        EmailChannelSetting? channelSetting = null)
    {
        setting ??= new SmtpProviderSetting { Host = "smtp.test.com", Port = 587, UseDefaultCredentials = false, Username = "user", Password = "pass", Enabled = true, Priority = 1 };
        channelSetting ??= new EmailChannelSetting { FromEmail = "from@test.com", FromName = "Test" };
        Mock<IOptions<SmtpProviderSetting>> optionsMock = new();
        optionsMock.Setup(x => x.Value).Returns(setting);
        Mock<IOptions<EmailChannelSetting>> channelOptionsMock = new();
        channelOptionsMock.Setup(x => x.Value).Returns(channelSetting);
        IFluentEmail fluentEmail = (fluentEmailMock ?? CreateBaseEmailMock()).Object;
        Mock<ILogger<SmtpProvider>> loggerMock = new();
        return new SmtpProvider(optionsMock.Object, channelOptionsMock.Object, fluentEmail, loggerMock.Object);
    }
    private static NotificationMessage CreateValidMessage()
    {
        return NotificationMessage.Create(
            NotificationUseCase.UserRegistered,
            NotificationRecipient.Create("recipient@test.com", "Test"),
            NotificationChannel.Email,
            NotificationContext.Create(
                (NotificationParameterType.UserFirstName, "Jane"),
                (NotificationParameterType.VerificationUrl, "https://example.com/activate"),
                (NotificationParameterType.ApplicationName, "TestSystem"),
                (NotificationParameterType.SupportEmail, "support@test.com"),
                (NotificationParameterType.UnsubscribeUrl, "https://example.com/unsubscribe")));
    }
    [Fact(DisplayName = "SendAsync with valid message should send and return Ok")]
    public async Task SendAsync_WithValidMessage_ShouldSendAndReturnOk()
    {
        SmtpProvider provider = CreateProvider();
        Result result = await provider.SendAsync(CreateValidMessage());
        result.IsSuccess.Should().BeTrue();
    }
    [Fact(DisplayName = "SendAsync with missing recipient should return RecipientMissing")]
    public async Task SendAsync_WithMissingRecipient_ShouldReturnRecipientMissing()
    {
        SmtpProvider provider = CreateProvider();
        NotificationMessage message = CreateValidMessage() with
        {
            Recipient = NotificationRecipient.Create(string.Empty)
        };
        Result result = await provider.SendAsync(message);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "Provider.Smtp.Recipient.Required");
    }
    [Fact(DisplayName = "SendAsync with missing Host should return ConfigurationMissing")]
    public async Task SendAsync_WithMissingHost_ShouldReturnConfigurationMissing()
    {
        SmtpProvider provider = CreateProvider(setting: new SmtpProviderSetting { Host = string.Empty, Port = 587, UseDefaultCredentials = false, Username = "user", Enabled = true, Priority = 1 });
        Result result = await provider.SendAsync(CreateValidMessage());
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code.Contains("Configuration") && e.Code.Contains("Host"));
    }
    [Fact(DisplayName = "SendAsync with invalid port should return ConfigurationMissing")]
    public async Task SendAsync_WithInvalidPort_ShouldReturnConfigurationMissing()
    {
        SmtpProvider provider = CreateProvider(setting: new SmtpProviderSetting { Host = "smtp.test.com", Port = 0, UseDefaultCredentials = false, Username = "user", Enabled = true, Priority = 1 });
        Result result = await provider.SendAsync(CreateValidMessage());
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code.Contains("Configuration") && e.Code.Contains("Port"));
    }
    [Fact(DisplayName = "SendAsync with missing credentials should return ConfigurationMissing")]
    public async Task SendAsync_WithMissingCredentials_ShouldReturnConfigurationMissing()
    {
        SmtpProvider provider = CreateProvider(setting: new SmtpProviderSetting { Host = "smtp.test.com", Port = 587, UseDefaultCredentials = false, Username = string.Empty, Enabled = true, Priority = 1 });
        Result result = await provider.SendAsync(CreateValidMessage());
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code.Contains("Configuration") && e.Code.Contains("Username"));
    }
    [Fact(DisplayName = "SendAsync with content mapping failure should propagate errors")]
    public async Task SendAsync_WithContentMappingFailure_ShouldPropagateErrors()
    {
        SmtpProvider provider = CreateProvider();
        NotificationMessage message = CreateValidMessage() with
        {
            UseCase = NotificationUseCase.None
        };
        Result result = await provider.SendAsync(message);
        result.IsSuccess.Should().BeFalse();
    }
    [Fact(DisplayName = "SendAsync when SMTP send fails should return SendFailed")]
    public async Task SendAsync_WhenSendFails_ShouldReturnSendFailed()
    {
        Mock<IFluentEmail> mockEmail = new();
        mockEmail.Setup(x => x.SetFrom(It.IsAny<string>(), It.IsAny<string>())).Returns(mockEmail.Object);
        mockEmail.Setup(x => x.To(It.IsAny<string>(), It.IsAny<string>())).Returns(mockEmail.Object);
        mockEmail.Setup(x => x.Subject(It.IsAny<string>())).Returns(mockEmail.Object);
        mockEmail.Setup(x => x.PlaintextAlternativeBody(It.IsAny<string>())).Returns(mockEmail.Object);
        mockEmail.Setup(x => x.Body(It.IsAny<string>(), It.IsAny<bool>())).Returns(mockEmail.Object);
        mockEmail.Setup(x => x.SendAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("SMTP error"));
        SmtpProvider provider = CreateProvider(fluentEmailMock: mockEmail);
        Result result = await provider.SendAsync(CreateValidMessage());
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code.Contains("SendFailed"));
    }
    [Fact(DisplayName = "SendAsync when exception thrown should return SendFailed")]
    public async Task SendAsync_WhenExceptionThrown_ShouldReturnSendFailed()
    {
        Mock<IFluentEmail> mockEmail = new();
        mockEmail.Setup(x => x.SetFrom(It.IsAny<string>(), It.IsAny<string>())).Returns(mockEmail.Object);
        mockEmail.Setup(x => x.To(It.IsAny<string>(), It.IsAny<string>())).Returns(mockEmail.Object);
        mockEmail.Setup(x => x.Subject(It.IsAny<string>())).Returns(mockEmail.Object);
        mockEmail.Setup(x => x.PlaintextAlternativeBody(It.IsAny<string>())).Returns(mockEmail.Object);
        mockEmail.Setup(x => x.Body(It.IsAny<string>(), It.IsAny<bool>())).Returns(mockEmail.Object);
        mockEmail.Setup(x => x.SendAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Connection refused"));
        SmtpProvider provider = CreateProvider(fluentEmailMock: mockEmail);
        Result result = await provider.SendAsync(CreateValidMessage());
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code.Contains("SendFailed"));
    }
}
