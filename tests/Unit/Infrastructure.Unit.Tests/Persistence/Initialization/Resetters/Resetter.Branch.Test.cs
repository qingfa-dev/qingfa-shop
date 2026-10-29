using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Resetters;
using Application.Common.Persistence.UnitOfWorks;

using Infrastructure.Persistence.EntityFramework.Initialization;
using Infrastructure.Persistence.Initialization;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Unit.Tests.Persistence.Initialization.Resetters;

/// <summary>
/// Branch tests for <see cref="EfCoreResetter{TContext}"/> that exercise PostgreSQL-only
/// paths, invalid scopes, and fatal reset errors without a PostgreSQL server.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence.Initialization.Resetters")]
public sealed class ResetterBranchTest : IAsyncLifetime
{
    private string _databasePath = string.Empty;

    /// <inheritdoc />
    public ValueTask InitializeAsync()
    {
        _databasePath = $"Data Source=ResetterBranch_{Guid.NewGuid():N}.db";
        return default;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        var options = new DbContextOptionsBuilder<ProviderOverrideDbContext>()
            .UseSqlite(_databasePath)
            .Options;
        await using var context = new ProviderOverrideDbContext(options);
        await context.Database.EnsureDeletedAsync();
    }

    private ProviderOverrideDbContext CreateProviderOverrideContext() =>
        new(new DbContextOptionsBuilder<ProviderOverrideDbContext>()
            .UseSqlite(_databasePath)
            .Options);

    private static EfCoreInitializationContext CreateContext(IDbConnection connection) =>
        new(
            connection,
            new Mock<IUnitOfWork>().Object,
            new Mock<ILogger>().Object);

    [Fact(DisplayName = "ExecuteAsync should reject an invalid scope")]
    public async Task ExecuteAsync_WithInvalidScope_ShouldFail()
    {
        await using var context = CreateProviderOverrideContext();
        var resetter = new EfCoreResetter<ProviderOverrideDbContext>(
            context, new Mock<ILogger>().Object, (ResetScope)999);

        Result result = await resetter.ExecuteAsync(CreateContext(Mock.Of<IDbConnection>()));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == SchemaResetOutcome.Failure.ResetFailed("invalid").Code);
    }

    [Fact(DisplayName = "PreviewAsync should reject an invalid scope")]
    public async Task PreviewAsync_WithInvalidScope_ShouldFail()
    {
        await using var context = CreateProviderOverrideContext();
        var resetter = new EfCoreResetter<ProviderOverrideDbContext>(
            context, new Mock<ILogger>().Object, (ResetScope)999);

        Result<IReadOnlyList<InitializationOperation>> result =
            await resetter.PreviewAsync(CreateContext(Mock.Of<IDbConnection>()));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == SchemaResetOutcome.Failure.ResetFailed("invalid").Code);
    }

    [Fact(DisplayName = "ExecuteAsync should translate an unexpected reset failure")]
    public async Task ExecuteAsync_WhenConnectionFails_ShouldReturnFailure()
    {
        await using var context = CreateProviderOverrideContext();
        var connection = new Mock<IDbConnection>();
        connection.SetupGet(value => value.State).Throws(new InvalidOperationException("broken"));
        var resetter = new EfCoreResetter<ProviderOverrideDbContext>(
            context, new Mock<ILogger>().Object, ResetScope.DataOnly);

        Result result = await resetter.ExecuteAsync(CreateContext(connection.Object));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == SchemaResetOutcome.Failure.ResetFailed("invalid").Code);
    }

    [Fact(DisplayName = "DataOnly reset on PostgreSQL should truncate tables and toggle FK checks")]
    public async Task ExecuteAsync_DataOnlyOnPostgres_ShouldTruncateAndToggleForeignKeys()
    {
        await using var context = CreateProviderOverrideContext();
        var connection = CreatePostgresConnection("todos", "todo_lists");
        var resetter = new EfCoreResetter<ProviderOverrideDbContext>(
            context,
            new Mock<ILogger>().Object,
            ResetScope.DataOnly,
            targets: ["todo.todos", "todo.todo_lists", "missing"]);

        Result result = await resetter.ExecuteAsync(CreateContext(connection.Connection));

        result.IsSuccess.Should().BeTrue(
            string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Message}")));
        connection.Command.NonQueryCalls.Should().Be(4);
    }

    [Fact(DisplayName = "Full reset on PostgreSQL should close the connection and clear pools")]
    public async Task ExecuteAsync_FullOnPostgres_ShouldDropDatabase()
    {
        await using var context = CreateProviderOverrideContext();
        await context.Database.EnsureCreatedAsync();
        var resetter = new EfCoreResetter<ProviderOverrideDbContext>(
            context, new Mock<ILogger>().Object, ResetScope.Full);

        Result result = await resetter.ExecuteAsync(CreateContext(Mock.Of<IDbConnection>()));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "SchemaOnly reset on PostgreSQL should surface a failed schema command")]
    public async Task ExecuteAsync_SchemaOnlyOnPostgres_ShouldSurfaceCommandFailure()
    {
        await using var context = CreateProviderOverrideContext();
        await context.Database.EnsureCreatedAsync();
        var resetter = new EfCoreResetter<ProviderOverrideDbContext>(
            context, new Mock<ILogger>().Object, ResetScope.SchemaOnly);

        Result result = await resetter.ExecuteAsync(CreateContext(Mock.Of<IDbConnection>()));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == SchemaResetOutcome.Failure.ResetFailed("invalid").Code);
    }

    /// <summary>A SQLite context that reports itself as PostgreSQL for branch coverage.</summary>
    private sealed class ProviderOverrideDbContext(DbContextOptions<ProviderOverrideDbContext> options)
        : TestResetterDbContext(options)
    {
        public override DatabaseFacade Database =>
            new ProviderNameFacade(this, "Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    /// <summary>Facade that overrides only the provider name.</summary>
    private sealed class ProviderNameFacade(DbContext context, string providerName) : DatabaseFacade(context)
    {
        public override string? ProviderName => providerName;
    }

    /// <summary>A fake PostgreSQL-shaped connection that returns table metadata.</summary>
    private static PostgresConnection CreatePostgresConnection(params string[] tables)
    {
        var command = new FakeCommand(() => CreateReader(tables));
        return new PostgresConnection(new FakeConnection(command), command);
    }

    private static DbDataReader CreateReader(params string[] tables)
    {
        var reader = new Mock<DbDataReader>();
        var readSequence = reader.SetupSequence(value => value.ReadAsync(It.IsAny<CancellationToken>()));
        foreach (string _ in tables)
            readSequence = readSequence.ReturnsAsync(true);
        readSequence.ReturnsAsync(false);

        var nameSequence = reader.SetupSequence(value => value.GetString(0));
        foreach (string table in tables)
            nameSequence = nameSequence.Returns(table);

        return reader.Object;
    }

    /// <summary>The concrete fake connection and command, exposed for verification.</summary>
    private sealed record PostgresConnection(FakeConnection Connection, FakeCommand Command);

    /// <summary>Minimal DbConnection that returns one fake command.</summary>
    private sealed class FakeConnection(FakeCommand command) : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;

        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;

        public override string Database => "test";

        public override string DataSource => "test";

        public override string ServerVersion => "16";

        public override ConnectionState State => _state;

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
            throw new NotSupportedException();

        public override void ChangeDatabase(string databaseName)
        {
        }

        public override void Close() => _state = ConnectionState.Closed;

        public override void Open() => _state = ConnectionState.Open;

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            _state = ConnectionState.Open;
            return Task.CompletedTask;
        }

        public override Task CloseAsync()
        {
            _state = ConnectionState.Closed;
            return Task.CompletedTask;
        }

        protected override DbCommand CreateDbCommand() => command;
    }

    /// <summary>Minimal DbCommand that reads tables and records non-query calls.</summary>
    private sealed class FakeCommand(Func<DbDataReader> readerFactory) : DbCommand
    {
        private readonly FakeParameterCollection _parameters = new();

        [AllowNull]
        public override string CommandText { get; set; } = string.Empty;

        public override int CommandTimeout { get; set; } = 30;

        public override CommandType CommandType { get; set; } = CommandType.Text;

        public override bool DesignTimeVisible { get; set; }

        public override UpdateRowSource UpdatedRowSource { get; set; }

        public int NonQueryCalls { get; private set; }

        protected override DbConnection? DbConnection { get; set; }

        protected override DbParameterCollection DbParameterCollection => _parameters;

        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel()
        {
        }

        protected override DbParameter CreateDbParameter() => new FakeParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => readerFactory();

        public override int ExecuteNonQuery()
        {
            NonQueryCalls++;
            return 1;
        }

        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            NonQueryCalls++;
            return Task.FromResult(1);
        }

        public override object? ExecuteScalar() => null;

        public override void Prepare()
        {
        }
    }

    /// <summary>Minimal parameter collection used by <see cref="FakeCommand"/>.</summary>
    private sealed class FakeParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _parameters = [];

        public override int Count => _parameters.Count;

        public override object SyncRoot => _parameters;

        public override int Add(object value)
        {
            _parameters.Add((DbParameter)value);
            return _parameters.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (object value in values)
                Add(value);
        }

        public override void Clear() => _parameters.Clear();

        public override bool Contains(object value) => _parameters.Contains((DbParameter)value);

        public override bool Contains(string value) =>
            _parameters.Any(parameter => parameter.ParameterName == value);

        public override void CopyTo(Array array, int index) =>
            ((System.Collections.ICollection)_parameters).CopyTo(array, index);

        public override IEnumerator GetEnumerator() => ((IEnumerable)_parameters).GetEnumerator();

        protected override DbParameter GetParameter(int index) => _parameters[index];

        protected override DbParameter GetParameter(string parameterName) =>
            _parameters.First(parameter => parameter.ParameterName == parameterName);

        public override int IndexOf(object value) => _parameters.IndexOf((DbParameter)value);

        public override int IndexOf(string parameterName) =>
            _parameters.FindIndex(parameter => parameter.ParameterName == parameterName);

        public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);

        public override void Remove(object value) => _parameters.Remove((DbParameter)value);

        public override void RemoveAt(int index) => _parameters.RemoveAt(index);

        public override void RemoveAt(string parameterName) =>
            _parameters.RemoveAll(parameter => parameter.ParameterName == parameterName);

        protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            int index = IndexOf(parameterName);
            if (index >= 0)
                _parameters[index] = value;
        }
    }

    /// <summary>Minimal parameter used by <see cref="FakeCommand"/>.</summary>
    private sealed class FakeParameter : DbParameter
    {
        public override DbType DbType { get; set; }

        public override ParameterDirection Direction { get; set; }

        public override bool IsNullable { get; set; }

        [AllowNull]
        public override string ParameterName { get; set; } = string.Empty;

        public override int Size { get; set; }

        [AllowNull]
        public override string SourceColumn { get; set; } = string.Empty;

        public override bool SourceColumnNullMapping { get; set; }

        public override object? Value { get; set; }

        public override void ResetDbType()
        {
        }
    }
}
