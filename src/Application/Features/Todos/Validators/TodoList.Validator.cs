using Application.Features.Todos.Commands;

using Core.Todos;

using FluentValidation;

namespace Application.Features.Todos.Validators;

/// <summary>Validates <see cref="CreateTodoListCommand"/>.</summary>
public sealed class CreateTodoListCommandValidator : AbstractValidator<CreateTodoListCommand>
{
    /// <summary>Initializes the rules.</summary>
    public CreateTodoListCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(TodoListConstant.Constraints.Name.MaxLength);

        RuleFor(command => command.Description)
            .MaximumLength(TodoListConstant.Constraints.Description.MaxLength);
    }
}

/// <summary>Validates <see cref="UpdateTodoListCommand"/>.</summary>
public sealed class UpdateTodoListCommandValidator : AbstractValidator<UpdateTodoListCommand>
{
    /// <summary>Initializes the rules.</summary>
    public UpdateTodoListCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();

        When(command => command.Name is not null, () =>
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(TodoListConstant.Constraints.Name.MaxLength);
        });

        RuleFor(command => command.Description)
            .MaximumLength(TodoListConstant.Constraints.Description.MaxLength);
    }
}

/// <summary>Validates <see cref="DeleteTodoListCommand"/>.</summary>
public sealed class DeleteTodoListCommandValidator : AbstractValidator<DeleteTodoListCommand>
{
    /// <summary>Initializes the rules.</summary>
    public DeleteTodoListCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

/// <summary>Validates <see cref="RestoreTodoListCommand"/>.</summary>
public sealed class RestoreTodoListCommandValidator : AbstractValidator<RestoreTodoListCommand>
{
    /// <summary>Initializes the rules.</summary>
    public RestoreTodoListCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}
