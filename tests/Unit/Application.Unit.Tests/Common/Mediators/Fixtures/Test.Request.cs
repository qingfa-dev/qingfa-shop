using FluentValidation;

using MediatR;

namespace Application.Unit.Tests.Common.Mediators.Fixtures;

public record TestRequest : IRequest<Result>
{
    public string Value { get; init; } = default!;
}

public record TestRequestWithValue : IRequest<Result<string>>
{
    public string Value { get; init; } = default!;
}

public record TestRequestMultipleValidations : IRequest<Result>
{
    public string Value { get; init; } = default!;
    public string Name { get; init; } = default!;
}

public class TestRequestHandler : IRequestHandler<TestRequest, Result>
{
    public Task<Result> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Ok());
    }
}

public class TestRequestWithValueHandler : IRequestHandler<TestRequestWithValue, Result<string>>
{
    public Task<Result<string>> Handle(TestRequestWithValue request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok(request.Value));
    }
}

public class TestRequestMultipleValidationsHandler : IRequestHandler<TestRequestMultipleValidations, Result>
{
    public Task<Result> Handle(TestRequestMultipleValidations request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Ok());
    }
}

public record TestRequestThrowing : IRequest<Result>;

public class TestRequestThrowingHandler : IRequestHandler<TestRequestThrowing, Result>
{
    public Task<Result> Handle(TestRequestThrowing request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("handler blew up");
    }
}

public class TestRequestValidator : AbstractValidator<TestRequest>
{
    public TestRequestValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("Value is required");
    }
}

public class TestRequestWithValueValidator : AbstractValidator<TestRequestWithValue>
{
    public TestRequestWithValueValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("Value is required")
            .MinimumLength(3)
            .WithMessage("Value must be at least 3 characters");
    }
}

public class TestRequestMultipleValidationsValidator : AbstractValidator<TestRequestMultipleValidations>
{
    public TestRequestMultipleValidationsValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("Value is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");
    }
}