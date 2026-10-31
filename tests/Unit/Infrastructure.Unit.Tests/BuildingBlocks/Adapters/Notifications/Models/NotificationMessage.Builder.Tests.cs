using Application.BuildingBlocks.Ports.Notifications.Templates;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Models;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationMessageBuilderTests
{
    private static readonly NotificationRecipient s_recipient = NotificationRecipient.Create("user@test.com", "Alice");
    private static readonly NotificationAttachment s_attachment = NotificationAttachment.Create("doc.pdf", [0x00], "application/pdf");
    private static readonly NotificationContext s_context = NotificationContext.Create(
        (NotificationParameterType.ApplicationName, "ReSys"),
        (NotificationParameterType.VerificationCode, "123456"));

    [Fact(DisplayName = "Create should return success with message and channel")]
    public void Create_ShouldReturnSuccessWithMessage()
    {
        Result<NotificationMessage> result = NotificationMessageBuilder.Create(
            NotificationUseCase.UserRegistered, s_recipient, NotificationChannel.Email);

        result.IsSuccess.Should().BeTrue();
        result.Value.UseCase.Should().Be(NotificationUseCase.UserRegistered);
        result.Value.Recipient.Should().Be(s_recipient);
        result.Value.Channel.Should().Be(NotificationChannel.Email);
    }

    [Fact(DisplayName = "ForUseCase().To() should return success with message")]
    public void ForUseCaseTo_ShouldReturnSuccessWithMessage()
    {
        Result<NotificationMessage> result = NotificationMessageBuilder
            .ForUseCase(NotificationUseCase.UserRegistered)
            .To(s_recipient, NotificationChannel.Email);

        result.IsSuccess.Should().BeTrue();
        result.Value.UseCase.Should().Be(NotificationUseCase.UserRegistered);
        result.Value.Channel.Should().Be(NotificationChannel.Email);
    }

    [Fact(DisplayName = "WithMetadata should add metadata to message")]
    public void WithMetadata_ShouldAddMetadata()
    {
        Result<NotificationMessage> result = NotificationMessageBuilder.Create(
            NotificationUseCase.TwoFactorCodeRequested, s_recipient, NotificationChannel.Email);

        result = result.WithMetadata((NotificationMetadata.Priority, NotificationPriorityLevel.High));

        result.IsSuccess.Should().BeTrue();
        result.Value.Metadata[NotificationMetadata.Priority].Should().Be(NotificationPriorityLevel.High);
    }

    [Fact(DisplayName = "WithChannel should set channel on message")]
    public void WithChannel_ShouldSetChannel()
    {
        Result<NotificationMessage> result = NotificationMessageBuilder.Create(
            NotificationUseCase.UserRegistered, s_recipient, NotificationChannel.Email);

        result = result.WithChannel(NotificationChannel.SMS);

        result.IsSuccess.Should().BeTrue();
        result.Value.Channel.Should().Be(NotificationChannel.SMS);
    }

    [Fact(DisplayName = "WithChannel on failure should propagate errors")]
    public void WithChannel_OnFailure_ShouldPropagateErrors()
    {
        Error error = Error.Validation("Test.Error", "Test failure");
        Result<NotificationMessage> failed = error;

        Result<NotificationMessage> result = failed.WithChannel(NotificationChannel.Email);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == "Test.Error");
    }

    [Fact(DisplayName = "AddAttachment should add attachment to message")]
    public void AddAttachment_ShouldAddAttachment()
    {
        Result<NotificationMessage> result = NotificationMessageBuilder.Create(
            NotificationUseCase.OrderConfirmed, s_recipient, NotificationChannel.Email);

        result = result.AddAttachment(s_attachment);

        result.IsSuccess.Should().BeTrue();
        result.Value.Attachments.Should().ContainSingle().Which.Should().Be(s_attachment);
    }

    [Fact(DisplayName = "AddAttachment multiple should add all attachments")]
    public void AddAttachment_Multiple_ShouldAddAll()
    {
        NotificationAttachment second = NotificationAttachment.Create("second.txt", [0x01], "text/plain");
        Result<NotificationMessage> result = NotificationMessageBuilder.Create(
            NotificationUseCase.OrderConfirmed, s_recipient, NotificationChannel.Email);

        result = result.AddAttachment(s_attachment).AddAttachment(second);

        result.IsSuccess.Should().BeTrue();
        result.Value.Attachments.Should().HaveCount(2);
    }

    [Fact(DisplayName = "WithContext should set context on message")]
    public void WithContext_ShouldSetContext()
    {
        Result<NotificationMessage> result = NotificationMessageBuilder.Create(
            NotificationUseCase.UserRegistered, s_recipient, NotificationChannel.Email);

        result = result.WithContext(s_context);

        result.IsSuccess.Should().BeTrue();
        result.Value.Context.Should().Be(s_context);
    }

    [Fact(DisplayName = "AddParam should add parameter to context")]
    public void AddParam_ShouldAddParameter()
    {
        Result<NotificationMessage> result = NotificationMessageBuilder.Create(
            NotificationUseCase.WelcomeSent, s_recipient, NotificationChannel.Email);

        result = result.AddParam(NotificationParameterType.UserFirstName, "Alice");

        result.IsSuccess.Should().BeTrue();
        result.Value.Context.GetValue(NotificationParameterType.UserFirstName).Should().Be("Alice");
    }

    [Fact(DisplayName = "Chain multiple operations should build complete message")]
    public void ChainMultiple_ShouldBuildCompleteMessage()
    {
        Result<NotificationMessage> result = NotificationMessageBuilder
            .Create(NotificationUseCase.OrderConfirmed, s_recipient, NotificationChannel.Email)
            .WithMetadata(
                (NotificationMetadata.Priority, NotificationPriorityLevel.High),
                (NotificationMetadata.Language, "en-US"))
            .WithChannel(NotificationChannel.SMS)
            .AddAttachment(s_attachment)
            .AddParam(NotificationParameterType.OrderNumber, "ORD-123")
            .AddParam(NotificationParameterType.OrderTotal, "$99.99");

        result.IsSuccess.Should().BeTrue();
        result.Value.Channel.Should().Be(NotificationChannel.SMS);
        result.Value.Metadata[NotificationMetadata.Priority].Should().Be(NotificationPriorityLevel.High);
        result.Value.Metadata[NotificationMetadata.Language].Should().Be("en-US");
        result.Value.Attachments.Should().ContainSingle();
        result.Value.Context.GetValue(NotificationParameterType.OrderNumber).Should().Be("ORD-123");
        result.Value.Context.GetValue(NotificationParameterType.OrderTotal).Should().Be("$99.99");
    }

    [Fact(DisplayName = "WithMetadata on failure should propagate errors")]
    public void WithMetadata_OnFailure_ShouldPropagateErrors()
    {
        Error error = Error.Validation("Test.Error", "Test failure");
        Result<NotificationMessage> failed = error;

        Result<NotificationMessage> result = failed.WithMetadata(
            (NotificationMetadata.Priority, NotificationPriorityLevel.High));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == "Test.Error");
    }

    [Fact(DisplayName = "AddAttachment on failure should propagate errors")]
    public void AddAttachment_OnFailure_ShouldPropagateErrors()
    {
        Error error = Error.Validation("Test.Error", "Test failure");
        Result<NotificationMessage> failed = error;

        Result<NotificationMessage> result = failed.AddAttachment(s_attachment);

        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "AddParam on failure should propagate errors")]
    public void AddParam_OnFailure_ShouldPropagateErrors()
    {
        Error error = Error.Validation("Test.Error", "Test failure");
        Result<NotificationMessage> failed = error;

        Result<NotificationMessage> result = failed.AddParam(NotificationParameterType.ApplicationName, "ReSys");

        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "CreateContext should return empty context")]
    public void CreateContext_ShouldReturnEmptyContext()
    {
        Result<NotificationContext> result = NotificationMessageBuilder.CreateContext();

        result.IsSuccess.Should().BeTrue();
        result.Value.Parameters.Should().BeEmpty();
    }

    [Fact(DisplayName = "AddParam on context result should add parameter")]
    public void AddParam_OnContextResult_ShouldAddParameter()
    {
        Result<NotificationContext> result = NotificationMessageBuilder
            .CreateContext()
            .AddParam(NotificationParameterType.ApplicationName, "ReSys");

        result.IsSuccess.Should().BeTrue();
        result.Value.GetValue(NotificationParameterType.ApplicationName).Should().Be("ReSys");
    }

    [Fact(DisplayName = "AddParams on context result should add multiple parameters")]
    public void AddParams_OnContextResult_ShouldAddMultipleParameters()
    {
        Dictionary<NotificationParameterType, string?> parameters = new()
        {
            { NotificationParameterType.ApplicationName, "ReSys" },
            { NotificationParameterType.SupportEmail, "support@test.com" },
        };

        Result<NotificationContext> result = NotificationMessageBuilder
            .CreateContext()
            .AddParams(parameters);

        result.IsSuccess.Should().BeTrue();
        result.Value.GetValue(NotificationParameterType.ApplicationName).Should().Be("ReSys");
        result.Value.GetValue(NotificationParameterType.SupportEmail).Should().Be("support@test.com");
    }
}
