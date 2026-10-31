using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Todos.Reminders.Models;

using Core.BuildingBlocks.Functional.Results;
using Core.Modules.Todos.Reminders;

using Infrastructure.Integration.Tests.BuildingBlocks.Messaging;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Projections;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Integration.Tests.Modules.Todos.EventSourcing;

/// <summary>End-to-end reminder event-store, projection and read-model flow.</summary>
[Collection("messaging")]
public sealed class TodoReminderFlowIntegrationTest(MessagingFixture fixture) : IAsyncLifetime
{
    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await fixture.CleanAsync();
        fixture.CreateScope();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await fixture.CleanAsync();
    }

    [Fact]
    public async Task Reminder_ShouldProjectScheduleSnoozeAndDismissIdempotently()
    {
        // Arrange
        using IServiceScope scope = fixture.Services.CreateScope();
        var repository = scope.ServiceProvider
            .GetRequiredService<IEventSourcedRepository<TodoReminder, Guid>>();
        var runner = scope.ServiceProvider.GetRequiredService<ProjectionRunner>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Result<TodoReminder> created = TodoReminder.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddHours(1),
            now);
        created.IsSuccess.Should().BeTrue();

        // Act
        (await repository.SaveAsync(created.Value)).IsSuccess.Should().BeTrue();
        (await runner.RunOnceAsync()).IsSuccess.Should().BeTrue();

        // Assert
        TodoReminderModel model = await context.Set<TodoReminderModel>().SingleAsync();
        model.Status.Should().Be(ReminderStatus.Scheduled);

        // Act: snooze
        Result<TodoReminder> loaded = await repository.LoadAsync(model.Id);
        loaded.IsSuccess.Should().BeTrue();
        loaded.Value.Snooze(now.AddHours(2), now).IsSuccess.Should().BeTrue();
        (await repository.SaveAsync(loaded.Value)).IsSuccess.Should().BeTrue();
        (await runner.RunOnceAsync()).IsSuccess.Should().BeTrue();

        // Assert
        await context.Entry(model).ReloadAsync();
        model.Status.Should().Be(ReminderStatus.Snoozed);
        model.SnoozeCount.Should().Be(1);

        // Act: dismiss, then replay the same checkpoint
        Result<TodoReminder> dismissed = await repository.LoadAsync(model.Id);
        dismissed.Value.Dismiss(now).IsSuccess.Should().BeTrue();
        (await repository.SaveAsync(dismissed.Value)).IsSuccess.Should().BeTrue();
        (await runner.RunOnceAsync()).IsSuccess.Should().BeTrue();
        (await runner.RunOnceAsync()).IsSuccess.Should().BeTrue();

        // Assert
        await context.Entry(model).ReloadAsync();
        model.Status.Should().Be(ReminderStatus.Dismissed);
        (await context.Set<TodoReminderModel>().CountAsync()).Should().Be(1);
    }
}
