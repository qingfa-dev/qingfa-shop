namespace Infrastructure.Integration.Tests.Persistence.Todos;

[CollectionDefinition("Todo EfCore")]
public sealed class TodoEfCoreCollection : ICollectionFixture<TodoEfCoreFixture>;
