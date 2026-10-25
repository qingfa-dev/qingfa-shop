namespace Core.UnitTests.Common.Monad.Results;

internal static class ResultStub
{
    public static Error NotFoundError()
        => Error.NotFound("test.not_found", "The test resource was not found");

    public static Error ConflictError()
        => Error.Conflict("test.conflict", "The test resource conflicts");
}