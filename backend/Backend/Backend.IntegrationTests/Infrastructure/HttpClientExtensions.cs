using System.Net.Http;
using System.Net.Http.Headers;

namespace Backend.IntegrationTests.Infrastructure;

// Små hjelpemetoder for HttpClient brukt i testene
public static class HttpClientExtensions
{
    // Legger automatisk på en Authorization-header med JWT-token
    public static void SetBearerToken(this HttpClient client, string jwt)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", jwt);
    }
}
