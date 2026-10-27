using Application.Features.Todos.Commands;

using Core.Todos;

using FluentValidation;

namespace Application.Features.Todos.Validators;

/// <summary>Validates <see cref="CreateTodoCategoryCommand"/>.</summary>
public sealed class CreateTodoCategoryCommandValidator : AbstractValidator<CreateTodoCategoryCommand>
{
    /// <summary>Initializes the rules.</summary>
    public CreateTodoCategoryCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(TodoCategoryConstant.Constraints.Name.MaxLength);

        RuleFor(command => command.Description)
            .MaximumLength(TodoCategoryConstant.Constraints.Description.MaxLength);
    }
}

/// <summary>Validates <see cref="UpdateTodoCategoryCommand"/>.</summary>
public sealed class UpdateTodoCategoryCommandValidator : AbstractValidator<UpdateTodoCategoryCommand>
{
    /// <summary>Initializes the rules.</summary>
    public UpdateTodoCategoryCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();

        When(command => command.Name is not null, () =>
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(TodoCategoryConstant.Constraints.Name.MaxLength);
        });

        RuleFor(command => command.Description)
            .MaximumLength(TodoCategoryConstant.Constraints.Description.MaxLength);
    }
}

/// <summary>Validates <see cref="DeleteTodoCategoryCommand"/>.</summary>
public sealed class DeleteTodoCategoryCommandValidator : AbstractValidator<DeleteTodoCategoryCommand>
{
    /// <summary>Initializes the rules.</summary>
    public DeleteTodoCategoryCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

/// <summary>Validates <see cref="RestoreTodoCategoryCommand"/>.</summary>
public sealed class RestoreTodoCategoryCommandValidator : AbstractValidator<RestoreTodoCategoryCommand>
{
    /// <summary>Initializes the rules.</summary>
    public RestoreTodoCategoryCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}
