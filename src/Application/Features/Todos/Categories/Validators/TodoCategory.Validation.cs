using Application.Common.Validation.FluentValidation;
using Application.Features.Todos.Categories.Models;

using Core.Todos.Categories;

using FluentValidation;

namespace Application.Features.Todos.Categories.Validators;

/// <summary>Reusable FluentValidation rules for category requests.</summary>
public static class TodoCategoryValidation
{
    /// <summary>Applies the category name and description rules to a request.</summary>
    /// <typeparam name="T">The use case being validated.</typeparam>
    /// <typeparam name="TRequest">The category request model.</typeparam>
    /// <param name="ruleBuilder">The rule builder for the request.</param>
    /// <param name="isPatch">
    /// When <c>true</c>, the name is only validated if it was supplied.
    /// </param>
    /// <returns>The same rule chain, so more rules can be appended.</returns>
    public static IRuleBuilderOptions<T, TRequest> ApplyTodoCategoryRules<T, TRequest>(
        this IRuleBuilder<T, TRequest> ruleBuilder,
        bool isPatch = false)
        where TRequest : TodoCategoryRequestModel
        => ruleBuilder.ChildRules(request =>
        {
            IRuleBuilderOptions<TRequest, string> name = request
                .RuleFor(model => model.Name)
                .ValidateWith(TodoCategoryValidator.ValidateName);

            if (isPatch)
            {
                name.When(model => model.Name is not null);
            }

            request.RuleFor(model => model.Description)
                .ValidateWith(TodoCategoryValidator.ValidateDescription);
        });
}
