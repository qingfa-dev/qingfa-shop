using System.Reflection;
using System.Reflection.Emit;

using Application.Common.Messaging.Bus;

using Core.Common.Domain.Events;
using Core.Todos.Items;

using Infrastructure.Integrations.Messaging.Serialization;

namespace Infrastructure.Unit.Tests.Integrations.Messaging.Serialization;

/// <summary>
/// Unit tests for <see cref="EventTypeRegistry"/>; covers assembly scanning and explicit registration.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Messaging.Serialization")]
public sealed class EventTypeRegistryTest
{
    [Fact(DisplayName = "Constructor should register domain events found in the scanned assembly")]
    public void Constructor_WithAssemblyContainingDomainEvents_ShouldRegisterThem()
    {
        // Arrange
        var sut = new EventTypeRegistry(typeof(TodoCreated).Assembly);

        // Act
        Type resolved = sut.Resolve(nameof(TodoCreated), 1);

        // Assert
        resolved.Should().Be<TodoCreated>();
    }

    [Fact(DisplayName = "Constructor should skip open generic domain events")]
    public void Constructor_WithOpenGenericDomainEvent_ShouldSkipIt()
    {
        // Arrange
        Assembly assembly = CreateAssemblyWithOpenGenericDomainEvent();

        // Act
        var sut = new EventTypeRegistry(assembly);

        // Assert
        Action act = () => sut.Resolve("OpenGenericDomainEvent", 1);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact(DisplayName = "Register should resolve an explicitly registered integration event")]
    public void Register_WithIntegrationEvent_ShouldResolveByStableIdentity()
    {
        // Arrange
        var sut = new EventTypeRegistry();
        sut.Register<RegistryIntegrationEvent>("registry.integration", 3);

        // Act
        Type resolved = sut.Resolve("registry.integration", 3);

        // Assert
        resolved.Should().Be<RegistryIntegrationEvent>();
    }

    [Fact(DisplayName = "Register should reject a different type for the same identity")]
    public void Register_WithDifferentTypeForSameIdentity_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = new EventTypeRegistry();
        sut.Register<RegistryIntegrationEvent>("registry.integration", 1);

        // Act
        Action act = () => sut.Register<OtherIntegrationEvent>("registry.integration", 1);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*already registered*");
    }

    [Fact(DisplayName = "Register should tolerate re-registering the same type")]
    public void Register_WithSameTypeForSameIdentity_ShouldStayResolvable()
    {
        // Arrange
        var sut = new EventTypeRegistry();
        sut.Register<RegistryIntegrationEvent>("registry.integration", 1);
        sut.Register<RegistryIntegrationEvent>("registry.integration", 1);

        // Act
        Type resolved = sut.Resolve("registry.integration", 1);

        // Assert
        resolved.Should().Be<RegistryIntegrationEvent>();
    }

    [Fact(DisplayName = "Resolve should throw when the event name is unknown")]
    public void Resolve_WithUnknownEventName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = new EventTypeRegistry();

        // Act
        Action act = () => sut.Resolve("unknown.event", 1);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*unknown.event*");
    }

    [Fact(DisplayName = "Resolve should throw when only the version differs")]
    public void Resolve_WithKnownNameAndUnknownVersion_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = new EventTypeRegistry();
        sut.Register<RegistryIntegrationEvent>("registry.integration", 1);

        // Act
        Action act = () => sut.Resolve("registry.integration", 2);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*registry.integration*");
    }

    [Fact(DisplayName = "Resolve should throw when the event name is blank")]
    public void Resolve_WithBlankEventName_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = new EventTypeRegistry();

        // Act
        Action act = () => sut.Resolve(" ", 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Register should reject a non-positive event version")]
    public void Register_WithNonPositiveVersion_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var sut = new EventTypeRegistry();

        // Act
        Action act = () => sut.Register<RegistryIntegrationEvent>("registry.integration", 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Register should reject a blank event name")]
    public void Register_WithBlankEventName_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = new EventTypeRegistry();

        // Act
        Action act = () => sut.Register<RegistryIntegrationEvent>("", 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>Emits a runtime assembly holding one open generic domain event.</summary>
    /// <remarks>
    /// Build: the test assembly itself cannot be scanned because it declares several test-only
    /// domain events that share the same simple name; the registry treats that as a collision.
    /// The emitted type implements <see cref="IDomainEvent"/> directly so no record plumbing
    /// (notably the abstract <c>&lt;Clone&gt;$</c> member) has to be produced.
    /// </remarks>
    private static Assembly CreateAssemblyWithOpenGenericDomainEvent()
    {
        var assemblyName = new AssemblyName($"OpenGenericEvents{Guid.NewGuid():N}");
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder module = assembly.DefineDynamicModule(assemblyName.Name!);

        TypeBuilder builder = module.DefineType(
            "OpenGenericDomainEvent",
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(object),
            [typeof(IDomainEvent)]);
        builder.DefineGenericParameters("T");
        builder.DefineDefaultConstructor(MethodAttributes.Public);

        DefineGetterOverride(
            builder,
            typeof(IHasMetadata<DefaultMetadata>).GetProperty(nameof(IHasMetadata.Metadata))!,
            OpCodes.Ldnull);
        DefineGetterOverride(
            builder,
            typeof(IDomainEvent).GetProperty(nameof(IDomainEvent.EventName))!,
            OpCodes.Ldnull);
        DefineGetterOverride(
            builder,
            typeof(IDomainEvent).GetProperty(nameof(IDomainEvent.EventVersion))!,
            OpCodes.Ldc_I4_0);

        _ = builder.CreateType();

        return assembly;
    }

    private static void DefineGetterOverride(TypeBuilder builder, PropertyInfo property, OpCode value)
    {
        MethodInfo interfaceMethod = property.GetMethod!;

        MethodBuilder getter = builder.DefineMethod(
            interfaceMethod.Name,
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig |
            MethodAttributes.SpecialName,
            interfaceMethod.ReturnType,
            Type.EmptyTypes);

        ILGenerator body = getter.GetILGenerator();
        body.Emit(value);
        body.Emit(OpCodes.Ret);

        builder.DefineMethodOverride(getter, interfaceMethod);
    }

    private sealed record RegistryIntegrationEvent : IIntegrationEvent
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();

        public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

        public string EventName => "registry.integration";

        public int Version => 1;

        public DefaultMetadata Metadata { get; init; } = DefaultMetadata.Create();
    }

    private sealed record OtherIntegrationEvent : IIntegrationEvent
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();

        public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

        public string EventName => "other.integration";

        public int Version => 1;

        public DefaultMetadata Metadata { get; init; } = DefaultMetadata.Create();
    }
}
