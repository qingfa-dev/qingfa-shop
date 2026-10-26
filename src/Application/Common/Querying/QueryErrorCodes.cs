namespace Application.Common.Querying;

/// <summary>Stable error codes for query parsing, validation, mapping, and execution.</summary>
/// <remarks>
/// Centralizes all error codes so they can be referenced in tests, logging, and contracts
/// without duplicating string literals.
/// </remarks>
public static class QueryErrorCodes
{
    // ---- Filter ----
    public const string FilterSyntax                = "Query.Filter.Syntax";
    public const string FilterValueInvalid          = "Query.Filter.ValueInvalid";
    public const string FilterTooLong               = "Query.Filter.TooLong";
    public const string FilterDepthExceeded         = "Query.Filter.DepthExceeded";
    public const string FilterTooManyConditions     = "Query.Filter.TooManyConditions";

    // ---- Field ----
    public const string FieldNotMapped              = "Query.Field.NotMapped";
    public const string FieldNotFilterable          = "Query.Field.NotFilterable";
    public const string FieldNotSortable            = "Query.Field.NotSortable";
    public const string FieldNotSelectable          = "Query.Field.NotSelectable";
    public const string FieldNotGroupable           = "Query.Field.NotGroupable";
    public const string FieldDuplicate              = "Query.Field.Duplicate";

    // ---- Operator ----
    public const string OperatorTypeMismatch        = "Query.Operator.TypeMismatch";
    public const string OperatorUnsupported         = "Query.Operator.Unsupported";

    // ---- Order ----
    public const string OrderSyntax                 = "Query.Order.Syntax";

    // ---- Page ----
    public const string PageInvalid                 = "Query.Page.Invalid";
    public const string PageSizeInvalid             = "Query.PageSize.Invalid";

    // ---- Search ----
    public const string SearchNotConfigured         = "Query.Search.NotConfigured";
    public const string SearchTooLong               = "Query.Search.TooLong";
    public const string SearchTooManyTerms          = "Query.Search.TooManyTerms";
    public const string SearchTooManyFields         = "Query.Search.TooManyFields";
    public const string SearchLanguageTooLong       = "Query.Search.LanguageTooLong";
    public const string SearchInvalidMinScore       = "Query.Search.InvalidMinScore";

    // ---- Tabular ----
    public const string TabularMixedShape           = "Query.Tabular.MixedShape";
    public const string TabularFieldNotGrouped      = "Query.Tabular.FieldNotGrouped";

    // ---- Aggregate ----
    public const string AggregateSyntax             = "Query.Aggregate.Syntax";
    public const string AggregateUnsupported        = "Query.Aggregate.Unsupported";

    // ---- Unsupported ----
    public const string Unsupported                 = "Query.Unsupported";

    // ---- Map Declaration ----
    public const string MapNameRequired             = "Query.Map.NameRequired";
    public const string MapNameTooLong              = "Query.Map.NameTooLong";
    public const string MapDuplicate                = "Query.Map.Duplicate";
    public const string MapSearchableNotString      = "Query.Map.SearchableNotString";
    public const string MapCompositeEmpty           = "Query.Map.CompositeEmpty";
    public const string MapCompositeTypeMismatch    = "Query.Map.CompositeTypeMismatch";

    // ---- Map Registry ----
    public const string MapAlreadyRegistered        = "Query.Map.AlreadyRegistered";
    public const string MapNotRegistered            = "Query.Map.NotRegistered";
}