using Application.Features.Todos.Commands;

using Core.Todos;

using FluentValidation;

namespace Application.Features.Todos.Validators;

/// <summary>Validates <see cref="CreateTodoCommand"/>.</summary>
public sealed class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
{
    /// <summary>Initializes the rules.</summary>
    public CreateTodoCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(TodoConstant.Constraints.Title.MaxLength);

        RuleFor(command => command.Description)
            .MaximumLength(TodoConstant.Constraints.Description.MaxLength);

        RuleFor(command => command.CategoryIds)
            .Must(ids => ids is null || ids.Count <= TodoConstant.Constraints.Collection.MaxCategories)
            .WithMessage(
                $"A todo cannot reference more than {TodoConstant.Constraints.Collection.MaxCategories} categories.");

        RuleForEach(command => command.CategoryIds)
            .NotEmpty();
    }
}

/// <summary>Validates <see cref="UpdateTodoCommand"/>.</summary>
public sealed class UpdateTodoCommandValidator : AbstractValidator<UpdateTodoCommand>
{
    /// <summary>Initializes the rules.</summary>
    public UpdateTodoCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();

        When(command => command.Title is not null, () =>
        {
            RuleFor(command => command.Title)
                .NotEmpty()
                .MaximumLength(TodoConstant.Constraints.Title.MaxLength);
        });

        RuleFor(command => command.Description)
            .MaximumLength(TodoConstant.Constraints.Description.MaxLength);
    }
}

/// <summary>Validates <see cref="StartTodoCommand"/>.</summary>
public sealed class StartTodoCommandValidator : AbstractValidator<StartTodoCommand>
{
    /// <summary>Initializes the rules.</summary>
    public StartTodoCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

/// <summary>Validates <see cref="CompleteTodoCommand"/>.</summary>
public sealed class CompleteTodoCommandValidator : AbstractValidator<CompleteTodoCommand>
{
    /// <summary>Initializes the rules.</summary>
    public CompleteTodoCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

/// <summary>Validates <see cref="CancelTodoCommand"/>.</summary>
public sealed class CancelTodoCommandValidator : AbstractValidator<CancelTodoCommand>
{
    /// <summary>Initializes the rules.</summary>
    public CancelTodoCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

/// <summary>Validates <see cref="ReopenTodoCommand"/>.</summary>
public sealed class ReopenTodoCommandValidator : AbstractValidator<ReopenTodoCommand>
{
    /// <summary>Initializes the rules.</summary>
    public ReopenTodoCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

/// <summary>Validates <see cref="ClearTodoListCommand"/>.</summary>
public sealed class ClearTodoListCommandValidator : AbstractValidator<ClearTodoListCommand>
{
    /// <summary>Initializes the rules.</summary>
    public ClearTodoListCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

/// <summary>Validates <see cref="DeleteTodoCommand"/>.</summary>
public sealed class DeleteTodoCommandValidator : AbstractValidator<DeleteTodoCommand>
{
    /// <summary>Initializes the rules.</summary>
    public DeleteTodoCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

/// <summary>Validates <see cref="RestoreTodoCommand"/>.</summary>
public sealed class RestoreTodoCommandValidator : AbstractValidator<RestoreTodoCommand>
{
    /// <summary>Initializes the rules.</summary>
    public RestoreTodoCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

/// <summary>Validates <see cref="AssignTodoToListCommand"/>.</summary>
public sealed class AssignTodoToListCommandValidator : AbstractValidator<AssignTodoToListCommand>
{
    /// <summary>Initializes the rules.</summary>
    public AssignTodoToListCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.ListId).NotEmpty();
    }
}

/// <summary>Validates <see cref="AddTodoCategoryCommand"/>.</summary>
public sealed class AddTodoCategoryCommandValidator : AbstractValidator<AddTodoCategoryCommand>
{
    /// <summary>Initializes the rules.</summary>
    public AddTodoCategoryCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.CategoryId).NotEmpty();
    }
}

/// <summary>Validates <see cref="RemoveTodoCategoryCommand"/>.</summary>
public sealed class RemoveTodoCategoryCommandValidator : AbstractValidator<RemoveTodoCategoryCommand>
{
    /// <summary>Initializes the rules.</summary>
    public RemoveTodoCategoryCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.CategoryId).NotEmpty();
    }
}
