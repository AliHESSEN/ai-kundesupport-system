// FIL: Backend.IntegrationTests/PingTests.cs
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Backend.IntegrationTests.Infrastructure; // <-- for CustomWebApplicationFactory

namespace Backend.IntegrationTests
{
    public class PingTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public PingTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Ping_returns_pong()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/ping");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }
    }
}
