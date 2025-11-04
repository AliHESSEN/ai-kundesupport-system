using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Backend.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Backend.IntegrationTests
{
    public class PingTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        // WebApplicationFactory spinner opp API-et i minnet og gir oss en HttpClient mot det
        public PingTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Ping_returns_pong()
        {
            // Act: kall endepunktet /ping
            var response = await _client.GetAsync("/ping");

            // Assert: HTTP 200 OK
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Bodyen er JSON med en streng ("pong"), så vi deserialiserer til string
            var body = await JsonSerializer.DeserializeAsync<string>(
                await response.Content.ReadAsStreamAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            body.Should().Be("pong");
        }
    }
}
