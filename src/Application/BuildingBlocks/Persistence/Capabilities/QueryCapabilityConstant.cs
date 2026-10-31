namespace Application.BuildingBlocks.Persistence.Capabilities;

/// <summary>Constants for query capabilities and limits.</summary>
public static class QueryCapabilityConstant
{
    /// <summary>Cross-cutting query limits.</summary>
    public static class Limits
    {
        /// <summary>Largest page size the provider accepts.</summary>
        public const int MaxPageSize = 10_000;
    }

    /// <summary>Limits on the filter text.</summary>
    public static class Filter
    {
        /// <summary>Longest filter text a request may send.</summary>
        public const int MaxLength = 2048;

        /// <summary>Deepest parenthesis nesting a filter may use.</summary>
        public const int MaxDepth = 32;

        /// <summary>Most conditions a filter may contain.</summary>
        public const int MaxConditions = 100;
    }

    /// <summary>Limits on the ordering text.</summary>
    public static class Order
    {
        /// <summary>Most fields a request may order by.</summary>
        public const int MaxFields = 8;
    }

    /// <summary>Limits on the projection text.</summary>
    public static class Select
    {
        /// <summary>Most columns a request may project.</summary>
        public const int MaxColumns = 32;
    }

    /// <summary>Limits on the grouping text.</summary>
    public static class Group
    {
        /// <summary>Most keys a request may group by.</summary>
        public const int MaxKeys = 8;
    }

    /// <summary>Limits on the aggregate text.</summary>
    public static class Aggregate
    {
        /// <summary>Most aggregates a request may ask for.</summary>
        public const int MaxCount = 16;
    }

    /// <summary>Limits on the search text.</summary>
    public static class Search
    {
        /// <summary>Longest search text a request may send.</summary>
        public const int MaxLength = 256;

        /// <summary>Most terms a request may submit.</summary>
        public const int MaxTerms = 8;

        /// <summary>Most fields a request may target for search.</summary>
        public const int MaxFields = 8;
    }

    /// <summary>Limits on a mapped field.</summary>
    public static class Map
    {
        /// <summary>Longest field name a map may declare.</summary>
        public const int MaxFieldNameLength = 64;
    }

    /// <summary>The tokens the filter grammar is built from.</summary>
    public static class Tokens
    {
        /// <summary>Comparison operators, exactly as they appear in a filter.</summary>
        public static class Operator
        {
            /// <summary>Equal comparison.</summary>
            public const string Equal = "=";

            /// <summary>Not-equal comparison.</summary>
            public const string NotEqual = "!=";

            /// <summary>Less-than comparison.</summary>
            public const string LessThan = "<";

            /// <summary>Greater-than comparison.</summary>
            public const string GreaterThan = ">";

            /// <summary>Greater-than-or-equal comparison.</summary>
            public const string GreaterThanOrEqual = ">=";

            /// <summary>Less-than-or-equal comparison.</summary>
            public const string LessThanOrEqual = "<=";

            /// <summary>Contains comparison.</summary>
            public const string Contains = "=*";

            /// <summary>Not-contains comparison.</summary>
            public const string NotContains = "!*";

            /// <summary>Starts-with comparison.</summary>
            public const string StartsWith = "^";

            /// <summary>Not-starts-with comparison.</summary>
            public const string NotStartsWith = "!^";

            /// <summary>Ends-with comparison.</summary>
            public const string EndsWith = "$";

            /// <summary>Not-ends-with comparison.</summary>
            public const string NotEndsWith = "!$";

            /// <summary>Prefix that names a custom operator the adapter must know.</summary>
            public const string Custom = "#";
        }

        /// <summary>Structural characters of the grammar.</summary>
        public static class Character
        {
            /// <summary>Separates the conditions of an AND group.</summary>
            public const char And = ',';

            /// <summary>Separates the conditions of an OR group.</summary>
            public const char Or = '|';

            /// <summary>Opens a nested group.</summary>
            public const char GroupOpen = '(';

            /// <summary>Closes a nested group.</summary>
            public const char GroupClose = ')';

            /// <summary>Escapes the character that follows it.</summary>
            public const char Escape = '\\';

            /// <summary>Marks a value as case-insensitive when followed by 'i'.</summary>
            public const char CaseInsensitive = '/';

            /// <summary>The letter that completes the case-insensitive marker.</summary>
            public const char CaseInsensitiveMarker = 'i';

            /// <summary>A space.</summary>
            public const char Space = ' ';

            /// <summary>A tab.</summary>
            public const char Tab = '\t';
        }

        /// <summary>Words the grammar recognises outside a value.</summary>
        public static class Keyword
        {
            /// <summary>Ascending direction in an ordering.</summary>
            public const string Ascending = "asc";

            /// <summary>Descending direction in an ordering.</summary>
            public const string Descending = "desc";

            /// <summary>The literal that asks for a null operand.</summary>
            public const string Null = "null";

            /// <summary>The suffix that asks for a case-insensitive comparison.</summary>
            public const string CaseInsensitive = "/i";

            /// <summary>The alias the adapter uses when it projects a relevance score.</summary>
            public const string Rank = "__rank";
        }

        /// <summary>Search-mode names accepted by the structured <c>SearchSpec</c>.</summary>
        public static class SearchMode
        {
            public const string Contains = "contains";
            public const string StartsWith = "startswith";
            public const string Exact = "exact";
            public const string FullText = "fulltext";
            public const string Fuzzy = "fuzzy";
        }

        /// <summary>Separators between the parts of a projection.</summary>
        public static class Separator
        {
            /// <summary>Separates a projected field from its alias.</summary>
            public const string Alias = " as ";

            /// <summary>Separates the parts of a nested member path.</summary>
            public const char Member = '.';
        }

        /// <summary>Characters that end a field name.</summary>
        public static readonly char[] FieldTerminators =
        [
            Operator.Equal[0],
            Operator.NotEqual[0],
            Operator.LessThan[0],
            Operator.GreaterThan[0],
            Operator.StartsWith[0],
            Operator.EndsWith[0],
            Operator.Custom[0],
            Character.And,
            Character.Or,
            Character.GroupOpen,
            Character.GroupClose,
            Character.Space,
            Character.Tab,
        ];

        /// <summary>Operators written as two characters.</summary>
        public static readonly string[] TwoCharacterOperators =
        [
            Operator.NotEqual,
            Operator.GreaterThanOrEqual,
            Operator.LessThanOrEqual,
            Operator.Contains,
            Operator.NotContains,
            Operator.NotStartsWith,
            Operator.NotEndsWith,
        ];

        /// <summary>Operators written as a single character.</summary>
        public static readonly string[] OneCharacterOperators =
        [
            Operator.Equal,
            Operator.LessThan,
            Operator.GreaterThan,
            Operator.StartsWith,
            Operator.EndsWith,
        ];
    }

    /// <summary>The aggregate functions the grammar accepts.</summary>
    public static class Functions
    {
        /// <summary>Sum of the matching values.</summary>
        public const string Sum = "sum";

        /// <summary>Average of the matching values.</summary>
        public const string Average = "avg";

        /// <summary>Smallest matching value.</summary>
        public const string Minimum = "min";

        /// <summary>Largest matching value.</summary>
        public const string Maximum = "max";

        /// <summary>Count of the matching values.</summary>
        public const string Count = "count";

        /// <summary>Count of the distinct matching values.</summary>
        public const string DistinctCount = "distinctcount";

        /// <summary>The pattern an aggregate expression must match.</summary>
        public const string AggregatePattern =
            @"^(?<fn>sum|avg|min|max|count|distinctcount)\((?<field>[A-Za-z_][A-Za-z0-9_.\[\]]*)\)$";
    }
}