using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Application.BuildingBlocks.Ports.Notifications.Templates;

using Sinch;
using Sinch.SMS;
using Sinch.SMS.Batches;
using Sinch.SMS.Batches.Send;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Channels.Sms.Providers;
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class SinchProviderBehaviorTests
{
    private static SinchProvider CreateProvider(
        Mock<ISinchClient>? sinchMock = null,
        SinchProviderSetting? setting = null)
    {
        setting ??= new SinchProviderSetting { SenderPhoneNumber = "+1234567890", Enabled = true, Priority = 1 };
        Mock<IOptions<SinchProviderSetting>> optionsMock = new();
        optionsMock.Setup(x => x.Value).Returns(setting);
        ISinchClient sinchClient = (sinchMock ?? CreateSinchMock()).Object;
        Mock<ILogger<SinchProvider>> loggerMock = new();
        return new SinchProvider(optionsMock.Object, sinchClient, loggerMock.Object);
    }
    private static Mock<ISinchClient> CreateSinchMock()
    {
        Mock<ISinchClient> mock = new();
        Mock<ISinchSms> smsMock = new();
        Mock<ISinchSmsBatches> batchesMock = new();
        smsMock.Setup(x => x.Batches).Returns(batchesMock.Object);
        mock.Setup(x => x.Sms).Returns(smsMock.Object);
        batchesMock.Setup(x => x.Send(It.IsAny<SendTextBatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IBatch>());
        return mock;
    }
    private static NotificationMessage CreateValidMessage()
    {
        return NotificationMessage.Create(
            NotificationUseCase.PasswordSetupRequested,
            NotificationRecipient.Create("+9876543210", "Test"),
            NotificationChannel.SMS,
            NotificationContext.Create(
                (NotificationParameterType.UserFirstName, "Jane"),
                (NotificationParameterType.VerificationCode, "123456"),
                (NotificationParameterType.ApplicationName, "TestSystem"),
                (NotificationParameterType.SupportPhone, "+1234567890")));
    }
    [Fact(DisplayName = "SendAsync with valid message should send and return Ok")]
    public async Task SendAsync_WithValidMessage_ShouldSendAndReturnOk()
    {
        SinchProvider provider = CreateProvider();
        Result result = await provider.SendAsync(CreateValidMessage());
        result.IsSuccess.Should().BeTrue();
    }
    [Fact(DisplayName = "SendAsync with missing recipient should return RecipientMissing")]
    public async Task SendAsync_WithMissingRecipient_ShouldReturnRecipientMissing()
    {
        SinchProvider provider = CreateProvider();
        NotificationMessage message = CreateValidMessage() with
        {
            Recipient = NotificationRecipient.Create(string.Empty)
        };
        Result result = await provider.SendAsync(message);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "Provider.Sinch.Recipient.Required");
    }
    [Fact(DisplayName = "SendAsync with missing SenderPhoneNumber should return ConfigurationMissing")]
    public async Task SendAsync_WithMissingSenderPhone_ShouldReturnConfigurationMissing()
    {
        SinchProvider provider = CreateProvider(setting: new SinchProviderSetting { SenderPhoneNumber = string.Empty, Enabled = true, Priority = 1 });
        Result result = await provider.SendAsync(CreateValidMessage());
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code.Contains("Configuration") && e.Code.Contains("SenderPhoneNumber"));
    }
    [Fact(DisplayName = "SendAsync with content mapping failure should propagate errors")]
    public async Task SendAsync_WithContentMappingFailure_ShouldPropagateErrors()
    {
        SinchProvider provider = CreateProvider();
        NotificationMessage message = CreateValidMessage() with
        {
            UseCase = NotificationUseCase.None
        };
        Result result = await provider.SendAsync(message);
        result.IsSuccess.Should().BeFalse();
    }
    [Fact(DisplayName = "SendAsync when Sinch API fails should return SendFailed")]
    public async Task SendAsync_WhenSinchApiFails_ShouldReturnSendFailed()
    {
        Mock<ISinchClient> mockSinch = new();
        Mock<ISinchSms> smsMock = new();
        Mock<ISinchSmsBatches> batchesMock = new();
        smsMock.Setup(x => x.Batches).Returns(batchesMock.Object);
        mockSinch.Setup(x => x.Sms).Returns(smsMock.Object);
        batchesMock.Setup(x => x.Send(It.IsAny<SendTextBatchRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API returned 400"));
        SinchProvider provider = CreateProvider(sinchMock: mockSinch);
        Result result = await provider.SendAsync(CreateValidMessage());
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code.Contains("SendFailed"));
    }
    [Fact(DisplayName = "SendAsync when exception thrown should return SendFailed")]
    public async Task SendAsync_WhenExceptionThrown_ShouldReturnSendFailed()
    {
        Mock<ISinchClient> mockSinch = new();
        Mock<ISinchSms> smsMock = new();
        Mock<ISinchSmsBatches> batchesMock = new();
        smsMock.Setup(x => x.Batches).Returns(batchesMock.Object);
        mockSinch.Setup(x => x.Sms).Returns(smsMock.Object);
        batchesMock.Setup(x => x.Send(It.IsAny<SendTextBatchRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Network error"));
        SinchProvider provider = CreateProvider(sinchMock: mockSinch);
        Result result = await provider.SendAsync(CreateValidMessage());
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code.Contains("SendFailed"));
    }
}
