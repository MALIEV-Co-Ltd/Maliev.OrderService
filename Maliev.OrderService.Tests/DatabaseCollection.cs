using System.Diagnostics.CodeAnalysis;

namespace Maliev.OrderService.Tests.Contract;

/// <summary>
/// xUnit collection definition for sharing a single TestWebApplicationFactory instance across all test classes.
/// This ensures the test database, Redis, and RabbitMQ containers are created once and reused.
/// </summary>
[CollectionDefinition("Database")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit collection definition conventionally ends with 'Collection'")]
public class DatabaseCollection : ICollectionFixture<TestWebApplicationFactory>
{
    // This class has no code, and is never instantiated. Its purpose is to define
    // the collection fixture for xUnit to share a TestWebApplicationFactory instance.
}
