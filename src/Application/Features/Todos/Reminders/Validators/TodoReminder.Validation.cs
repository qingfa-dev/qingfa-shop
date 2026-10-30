using Application.Common.Validation.FluentValidation;
using Application.Features.Todos.Reminders.Models;

using Core.Todos.Reminders;

using FluentValidation;

namespace Application.Features.Todos.Reminders.Validators;

/// <summary>Reusable FluentValidation rules for reminder requests.</summary>
public static class TodoReminderValidation
{
    /// <summary>Applies the reminder field rules to a request.</summary>
    /// <typeparam name="T">The use case being validated.</typeparam>
    /// <typeparam name="TRequest">The reminder request model.</typeparam>
    /// <param name="ruleBuilder">The rule builder for the request.</param>
    /// <returns>The same rule chain, so more rules can be appended.</returns>
    public static IRuleBuilderOptions<T, TRequest> ApplyTodoReminderRules<T, TRequest>(
        this IRuleBuilder<T, TRequest> ruleBuilder)
        where TRequest : TodoReminderParameterModel
        => ruleBuilder.ChildRules(request =>
        {
            request.RuleFor(model => model.RemindAtUtc)
                .ValidateWith(TodoReminderValidator.ValidateRemindAtSpecified);
        });
}
