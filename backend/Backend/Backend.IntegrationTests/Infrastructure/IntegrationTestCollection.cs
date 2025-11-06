using Xunit;

namespace Backend.IntegrationTests.Infrastructure;

// Definerer en test-collection for integrasjonstestene
// Testklasser i samme collection deler samme WebApplicationFactory
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    // Tom klasse – XUnit bruker bare metadataen.
}
