// Dette interface definerer "kontrakten" for AI-tjenesten.
// Det betyr at alle klasser som implementerer IAIService,
// må ha en metode som heter AskAsync og som fungerer på denne måten.
public interface IAIService
{
    // AskAsync representerer et spørsmål som sendes til AI-modellen.
    // Den tar inn en tekst (question) og returnerer AI sitt svar som en string.
    // CancellationToken gjør det mulig å avbryte forespørselen hvis nødvendig.
    Task<string> AskAsync(string question, CancellationToken cancellationToken = default);
}
