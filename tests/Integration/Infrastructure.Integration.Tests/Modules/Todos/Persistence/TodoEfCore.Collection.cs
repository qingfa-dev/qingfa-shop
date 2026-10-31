namespace Infrastructure.Integration.Tests.Modules.Todos.Persistence;

[CollectionDefinition("Todo EfCore")]
public sealed class TodoEfCoreCollection : ICollectionFixture<TodoEfCoreFixture>;
