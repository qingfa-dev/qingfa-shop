using Application.BuildingBlocks.UseCases.Validation.FluentValidation;
using Application.Modules.Todos.Items.Models;

using Core.Modules.Todos.Items;

using FluentValidation;

namespace Application.Modules.Todos.Items.Validators;

/// <summary>Reusable FluentValidation rules for todo requests.</summary>
public static class TodoValidation
{
    /// <summary>Applies the todo field rules to a request.</summary>
    /// <typeparam name="T">The use case being validated.</typeparam>
    /// <typeparam name="TRequest">The todo request model.</typeparam>
    /// <param name="ruleBuilder">The rule builder for the request.</param>
    /// <param name="isPatch">
    /// When <c>true</c>, the title is only validated if it was supplied and
    /// category references are left to the use case that changes them.
    /// </param>
    /// <returns>The same rule chain, so more rules can be appended.</returns>
    public static IRuleBuilderOptions<T, TRequest> ApplyTodoRules<T, TRequest>(
        this IRuleBuilder<T, TRequest> ruleBuilder,
        bool isPatch = false)
        where TRequest : TodoRequestModel
        => ruleBuilder.ChildRules(request =>
        {
            IRuleBuilderOptions<TRequest, string> title = request
                .RuleFor(model => model.Title)
                .ValidateWith(TodoValidator.ValidateTitle);

            if (isPatch)
            {
                title.When(model => model.Title is not null);
            }

            request.RuleFor(model => model.Description)
                .ValidateWith(TodoValidator.ValidateDescription);

            request.RuleFor(model => model.ListId)
                .ValidateWith(TodoValidator.ValidateListId);

            if (!isPatch)
            {
                request.RuleFor(model => model.CategoryIds)
                    .ValidateWith(ids => TodoValidator.ValidateCategoryIds(ids.ToList()));
            }
        });
}
