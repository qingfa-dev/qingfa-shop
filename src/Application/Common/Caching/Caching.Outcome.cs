using Core.Common.Monad.Errors;

namespace Application.Common.Caching;

/// <summary>Failure outcomes for the cache port's contract.</summary>
public static class CachingOutcome
{
    /// <summary>Failure outcomes.</summary>
    public static class Failure
    {
        /// <summary>The caller supplied no key.</summary>
        public static Error KeyRequired => Error.BadRequest(
            code: "Caching.Key.Required",
            message: "A cache key is required.");

        /// <summary>The caller supplied no factory.</summary>
        public static Error FactoryRequired => Error.InternalServerError(
            code: "Caching.Factory.Required",
            message: "A cache factory is required.");

        /// <summary>The caller supplied no value to store.</summary>
        public static Error ValueRequired => Error.BadRequest(
            code: "Caching.Value.Required",
            message: "A cache value is required.");

        /// <summary>The caller supplied no tag.</summary>
        public static Error TagRequired => Error.BadRequest(
            code: "Caching.Tag.Required",
            message: "A cache tag is required.");
    }
}