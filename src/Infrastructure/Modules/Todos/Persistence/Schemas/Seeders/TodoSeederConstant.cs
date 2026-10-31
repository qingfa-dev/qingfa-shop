namespace Infrastructure.Modules.Todos.Persistence.Schemas.Seeders;

/// <summary>Stable identities, ordering, and bounds for the Todos seeders.</summary>
public static class TodoSeederConstant
{
    /// <summary>Seeder names used by registration and <c>DependsOn</c>.</summary>
    public static class Names
    {
        /// <summary>Category seeder name.</summary>
        public const string Categories = "seeder:todo-categories";

        /// <summary>List seeder name.</summary>
        public const string Lists = "seeder:todo-lists";

        /// <summary>Todo seeder name.</summary>
        public const string Todos = "seeder:todos";

        /// <summary>Backfill seeder name.</summary>
        public const string Backfill = "seeder:todo-backfill";
    }

    /// <summary>Execution order for the Todos seeders.</summary>
    public static class Order
    {
        /// <summary>Category seeder order.</summary>
        public const int Categories = 10;

        /// <summary>List seeder order.</summary>
        public const int Lists = 20;

        /// <summary>Todo seeder order.</summary>
        public const int Todos = 30;

        /// <summary>Backfill seeder order.</summary>
        public const int Backfill = 35;
    }

    /// <summary>Batch bounds for the backfill seeder.</summary>
    public static class Limits
    {
        /// <summary>Rows fetched per backfill page.</summary>
        public const int PageSize = 100;

        /// <summary>Upper bound that keeps one initialization pass bounded.</summary>
        public const int MaxBatchSize = 10_000;
    }
}
