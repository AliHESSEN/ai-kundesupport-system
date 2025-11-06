using System.Net;
using System.Threading.Tasks;
using Xunit;
using Backend.IntegrationTests.Infrastructure; // for CustomWebApplicationFactory + TestAuthHelpers

namespace Backend.IntegrationTests
{
    public class AuthSmokeTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AuthSmokeTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task WhoAmI_returns_claims_when_authenticated()
        {
            // Bruk korrekt metode
            var (client, _) = await TestAuthHelpers.CreateClientWithUserRoleAsync(_factory, "User");

            var resp = await client.GetAsync("/whoami");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }
    }
}
