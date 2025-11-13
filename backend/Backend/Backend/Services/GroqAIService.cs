using System.Net.Http.Headers;
using System.Text.Json;

// AI-tjenesten som snakker med Groq API
public class GroqAIService : IAIService
{
    private readonly HttpClient _httpClient; // brukes til å sende HTTP-forespørsler
    private readonly string _apiKey;         // API-nøkkelen vår

    public GroqAIService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["Groq:ApiKey"]!; // henter API-nøkkel fra config

        _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/"); // base-URL til Groq API
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey); // legger til auth-header
    }

    public async Task<string> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        // Body som sendes til AI-modellen
        var body = new
        {
            model = "openai/gpt-oss-20b", // hvilken modell vi bruker
            messages = new[]
            {
                new { role = "user", content = question } // spørsmålet fra brukeren
            }
        };

        // Sender POST-forespørsel til Groq
        var response = await _httpClient.PostAsJsonAsync(
            "chat/completions",
            body,
            cancellationToken
        );

        response.EnsureSuccessStatusCode(); // kaster feil hvis API-et svarer med error

        // Leser JSON-svaret fra AI
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        // Henter ut AI-svaret
        return json.RootElement
                   .GetProperty("choices")[0]
                   .GetProperty("message")
                   .GetProperty("content")
                   .GetString()!;
    }
}
