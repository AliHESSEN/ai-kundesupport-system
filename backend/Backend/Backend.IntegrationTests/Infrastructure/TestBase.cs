using System.Net.Http.Headers;
using Backend.IntegrationTests.Infrastructure;

namespace Backend.IntegrationTests.Infrastructure
{
    public abstract class TestBase : IClassFixture<CustomWebApplicationFactory>
    {
        protected readonly CustomWebApplicationFactory Factory;
        protected HttpClient Client = default!;
        protected string? CurrentUserId;

        public TestBase(CustomWebApplicationFactory factory)
        {
            Factory = factory;
            Client = factory.CreateClient(); // start uten token
        }

        // bruk riktig metode!
        protected async Task AuthenticateAsAsync(string role)
        {
            var (client, userId) = await TestAuthHelpers.CreateClientWithUserRoleAsync(Factory, role);
            Client = client;
            CurrentUserId = userId;
        }

        // Fjern auth hvis en test trenger å verifisere 401 uten token
        protected void ClearAuth()
        {
            Client.DefaultRequestHeaders.Authorization = null;
        }
    }
}
