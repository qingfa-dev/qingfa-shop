using Core.Common.Monad.Errors;

namespace Core.Common.Querying;

/// <summary>Error codes for query parsing and validation.</summary>
public static class QueryOutcome
{
    public static class Failure
    {
        public static Error Syntax(string detail) => Error.BadRequest(
            code: "Query.Filter.Syntax",
            message: $"Invalid filter syntax: {detail}.");

        public static Error ValueInvalid(string field, string value) => Error.BadRequest(
            code: "Query.Filter.ValueInvalid",
            message: $"Value '{value}' is not valid for field '{field}'.");

        public static Error TooLong(int max) => Error.BadRequest(
            code: "Query.Filter.TooLong",
            message: $"Filter cannot exceed {max} characters.");

        public static Error DepthExceeded(int max) => Error.BadRequest(
            code: "Query.Filter.DepthExceeded",
            message: $"Filter nesting cannot exceed {max} levels.");

        public static Error TooManyConditions(int max) => Error.BadRequest(
            code: "Query.Filter.TooManyConditions",
            message: $"Filter cannot contain more than {max} conditions.");

        public static Error NotMapped(string field) => Error.BadRequest(
            code: "Query.Field.NotMapped",
            message: $"Field '{field}' is not mapped.");

        public static Error NotFilterable(string field) => Error.BadRequest(
            code: "Query.Field.NotFilterable",
            message: $"Field '{field}' cannot be filtered.");

        public static Error NotSortable(string field) => Error.BadRequest(
            code: "Query.Field.NotSortable",
            message: $"Field '{field}' cannot be sorted.");

        public static Error NotSelectable(string field) => Error.BadRequest(
            code: "Query.Field.NotSelectable",
            message: $"Field '{field}' cannot be selected.");

        public static Error NotGroupable(string field) => Error.BadRequest(
            code: "Query.Field.NotGroupable",
            message: $"Field '{field}' cannot be grouped.");

        public static Error TypeMismatch(string field, string operatorName) => Error.BadRequest(
            code: "Query.Operator.TypeMismatch",
            message: $"Operator '{operatorName}' is not valid for field '{field}'.");

        public static Error OperatorUnsupported(string name) => Error.BadRequest(
            code: "Query.Operator.Unsupported",
            message: $"Operator '{name}' is not supported by this provider.");

        public static Error OrderSyntax(string detail) => Error.BadRequest(
            code: "Query.Order.Syntax",
            message: $"Invalid ordering: {detail}.");

        public static Error PageInvalid => Error.BadRequest(
            code: "Query.Page.Invalid",
            message: "Page must be greater than zero.");

        public static Error PageSizeInvalid(int max) => Error.BadRequest(
            code: "Query.PageSize.Invalid",
            message: $"Page size must be between 1 and {max}.");

        public static Error SearchNotConfigured => Error.BadRequest(
            code: "Query.Search.NotConfigured",
            message: "No searchable field is mapped.");

        public static Error SearchTooLong(int max) => Error.BadRequest(
            code: "Query.Search.TooLong",
            message: $"Search cannot exceed {max} characters.");

        public static Error MixedShape => Error.BadRequest(
            code: "Query.Tabular.MixedShape",
            message: "Select and aggregate cannot be combined without a group by.");

        public static Error FieldNotGrouped(string field) => Error.BadRequest(
            code: "Query.Tabular.FieldNotGrouped",
            message: $"Selected field '{field}' is not a grouping key.");

        public static Error AggregateSyntax(string detail) => Error.BadRequest(
            code: "Query.Aggregate.Syntax",
            message: $"Invalid aggregate: {detail}.");

        public static Error AggregateUnsupported(string field) => Error.BadRequest(
            code: "Query.Aggregate.Unsupported",
            message: $"Aggregate is not supported for field '{field}'.");

        public static Error Unsupported(string feature) => Error.BadRequest(
            code: "Query.Unsupported",
            message: $"The selected provider does not support {feature}.");
    }
}