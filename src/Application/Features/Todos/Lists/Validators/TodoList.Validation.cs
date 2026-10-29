using Application.Common.Validation.FluentValidation;
using Application.Features.Todos.Lists.Models;

using Core.Todos.Lists;

using FluentValidation;

namespace Application.Features.Todos.Lists.Validators;

/// <summary>Reusable FluentValidation rules for list requests.</summary>
public static class TodoListValidation
{
    /// <summary>Applies the list name and description rules to a request.</summary>
    /// <typeparam name="T">The use case being validated.</typeparam>
    /// <typeparam name="TRequest">The list request model.</typeparam>
    /// <param name="ruleBuilder">The rule builder for the request.</param>
    /// <param name="isPatch">
    /// When <c>true</c>, the name is only validated if it was supplied.
    /// </param>
    /// <returns>The same rule chain, so more rules can be appended.</returns>
    public static IRuleBuilderOptions<T, TRequest> ApplyTodoListRules<T, TRequest>(
        this IRuleBuilder<T, TRequest> ruleBuilder,
        bool isPatch = false)
        where TRequest : TodoListRequestModel
        => ruleBuilder.ChildRules(request =>
        {
            IRuleBuilderOptions<TRequest, string> name = request
                .RuleFor(model => model.Name)
                .ValidateWith(TodoListValidator.ValidateName);

            if (isPatch)
            {
                name.When(model => model.Name is not null);
            }

            request.RuleFor(model => model.Description)
                .ValidateWith(TodoListValidator.ValidateDescription);
        });
}
