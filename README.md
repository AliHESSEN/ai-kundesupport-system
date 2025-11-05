# Kundesupport-system med AI-integrasjon

Et fullstack websystem for håndtering av supportsaker med integrert AI-chatbot. Prosjektet er utviklet for å demonstrere hele utviklingspipen – fra idé og arkitektur til produksjonssetting – med moderne teknologier brukt i næringslivet.

## Formål

Systemet skal effektivisere kundesupport gjennom:

- Et brukergrensesnitt for å opprette og administrere saker
- AI-chatbot (OpenAI) som svarer på spørsmål
- Fallback til menneskelig support
- Rollebasert tilgang for agenter og administratorer

## Teknologistack

| Delområde         | Teknologi                             |
|-------------------|----------------------------------------|
| Backend           | C# / ASP.NET Core (Minimal API)       |
| Frontend          | React (Next.js) + TypeScript          |
| Database          | PostgreSQL + Entity Framework Core    |
| AI-integrasjon    | OpenAI API (GPT-4 / GPT-3.5)          |
| Autentisering     | ASP.NET Identity + JWT                |
| Logging/Monitoring| Grafana Cloud / Sentry.io             |
| CI/CD             | GitHub Actions                        |
| Containerisering  | Docker                                |
| Hosting           | Azure / Railway / Render              |

## Funksjoner

- Brukerautentisering og roller
- AI-chatbot for automatiserte svar
- Implementere RAG (Retrieval-Augmented Generation) ved å bruke pgvector i PostgreSQL for å lagre embeddings fra supportsaker. Dette vil gi AI-chatboten kontekstsensitive svar basert på tidligere henvendelser og intern kunnskap.
- Opprettelse og visning av supportsaker
- Endring av sakstatus (kun SupportStaff og Admin)
- Rollebasert tilgangsstyring direkte i API
- Admin-dashboard for oversikt og styring
- Logging og overvåkning
- CI/CD med testing og staging
- Produksjonsklar med Docker og cloud-deploy

## Tilgangskontroll og rollelogikk

Applikasjonen benytter **JWT-basert autentisering** og **rollebasert autorisasjon** for å sikre at brukere kun har tilgang til relevante data og funksjoner:

- **User**: Kan opprette og se sine egne saker.
- **SupportStaff**: Har tilgang til alle saker og kan endre status på dem.
- **Admin**: Har full tilgang, inkludert til å registrere nye brukere med roller.




- Hvis bruker ikke har rollen `Admin` eller `SupportStaff`, returnerer API-et `403 Forbidden`.
- Validering av statusfeltet håndteres i en dedikert DTO (Data Transfer Object).

## Sikkerhetstiltak

| Område                    | Tiltak                                                                 |
|---------------------------|------------------------------------------------------------------------|
| Inputvalidering           | DataAnnotations benyttes for automatisk validering av API-inndata      |
| Autentisering og autorisasjon | ASP.NET Identity med JWT og rollebasert tilgangskontroll         |
| Secrets management        | API-nøkler og connection strings håndteres via miljøvariabler          |
| Sårbarhetsskanning        | GitHub Dependabot og Snyk integrert i CI/CD-pipelinen                  |
| Statisk kodeanalyse       | SonarQube eller tilsvarende anbefales for kodekvalitetsanalyse         |
| Secret scanning           | GitHub Secret Scanning er aktivert                                     |
| HTTPS enforcement         | Alle API-kall håndteres over HTTPS                                     |
| Logging                   | Kun nødvendige feil logges – aldri sensitive data                      |
| Penetrasjonstesting       | Egen plan for testing før produksjonssetting                           |
| Patch management          | Alle avhengigheter oppdateres jevnlig med Dependabot                   |

## Dokumentasjon og videre utvikling

- API-endepunkter er beskrevet direkte i koden med forklarende kommentarer.
- Kodebasen følger prinsipper for minimal API-design, med tydelig separasjon av ansvar.
- I prosjektets sluttrapport inngår en teknisk vedlegg med detaljerte beskrivelser av:
  - DTO-er
  - Claims og rolleuttrekk fra JWT
  - Tilgangsnivå og sikkerhetskontroller
  - Logging og sporbarhet
    


- Frontend og brukeropplevelse
  
- Implementere WebSocket-basert oppdatering slik at supportsaker oppdateres i sanntid uten refresh.
- Utvikle et agent-dashboard med live-notifikasjoner når nye saker opprettes eller status endres.




## Integrasjonstester (Backend)

Integrasjonstestene verifiserer at hele backend-applikasjonen – inkludert API-endepunkter, autentisering og database – fungerer som forventet i et realistisk miljø.  
Testene kjøres automatisk mot en **isolert SQLite-database** og starter opp hele web-API-et via `WebApplicationFactory`.

### Oppsett og arkitektur

| Komponent | Teknologi / Rammeverk |
|------------|------------------------|
| **Testrammeverk** | xUnit |
| **Assertion-bibliotek** | FluentAssertions |
| **Test-API** | Microsoft.AspNetCore.Mvc.Testing |
| **Database (Testing)** | SQLite (via Entity Framework Core) |
| **Autentisering (Testing)** | Mocket JWT (testnøkkel lagret i minnet) |
| **Miljø** | `Testing` (settes automatisk under testkjøring) |


### Testdatabase og videre planer

- SQLite (Testing):
  Brukes som testdatabase for integrasjonstester. Lettvekts, rask og enkel å kjøre i et isolert miljø. Godt egnet for autentisering, autorisasjon, validering og vanlige CRUD-operasjoner.

- PostgreSQL + Testcontainers:
  Skal brukes for avanserte tester som krever ekte PostgreSQL-funksjonalitet, inkludert pgvector, komplekse spørringer, migrasjoner og RAG/AI-relaterte operasjoner.

---

### CustomWebApplicationFactory

Applikasjonen bruker `CustomWebApplicationFactory` for å konfigurere miljøet og testinnstillingene.  
Denne klassen spinner opp hele backend-API-et i et eget testmiljø og injiserer følgende konfigurasjon direkte i minnet:

```csharp
["ConnectionStrings:TestConnection"] = "Data Source=TestDb.sqlite";
["JwtSettings:SecretKey"] = "TestSigningKey123!";
["JwtSettings:Issuer"] = "TestIssuer";
["JwtSettings:Audience"] = "TestAudience";



---


