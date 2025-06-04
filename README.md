
# Kundesupport-system med AI-integrasjon

Et fullstack websystem for håndtering av supportsaker med integrert AI-chatbot. Prosjektet er utviklet for å demonstrere hele utviklingspipen – fra idé og arkitektur til produksjonssetting – med moderne teknologier brukt i norsk næringsliv.

## Formål

Systemet skal effektivisere kundesupport gjennom:
- Et brukergrensesnitt for å opprette og administrere saker
- AI-chatbot (OpenAI) som svarer på spørsmål
- Fallback til menneskelig support
- Rollebasert tilgang for agenter og administratorer

## Teknologistack

| Delområde          | Teknologi                         |
|--------------------|------------------------------------|
| Backend            | C# / ASP.NET Core (Minimal API)    |
| Frontend           | React (Next.js) + TypeScript       |
| Database           | PostgreSQL + Entity Framework Core |
| AI-integrasjon     | OpenAI API (GPT-4 / GPT-3.5)        |
| Autentisering      | ASP.NET Identity + JWT             |
| Logging/Monitoring | Grafana Cloud / Sentry.io          |
| CI/CD              | GitHub Actions                     |
| Containerisering   | Docker                             |
| Hosting            | Azure / Railway / Render           |

## Funksjoner

- [x] Brukerautentisering og roller
- [x] AI-chatbot for automatiserte svar
- [x] Opprettelse og visning av supportsaker
- [x] Admin-dashboard for oversikt og styring
- [x] Logging og overvåkning
- [x] CI/CD med testing og staging
- [x] Produksjonsklar med Docker og cloud-deploy




## Sikkerhetstiltak







| Område                        | Tiltak                                                                                                    |
| ----------------------------- | --------------------------------------------------------------------------------------------------------- |
| Inputvalidering               | DataAnnotations benyttes for automatisk validering av API-inndata (f.eks. Required, MaxLength).           |
| Autentisering og autorisasjon | ASP.NET Identity med JWT og rollebasert tilgangskontroll.                                                 |
| Secrets management            | API-nøkler og connection strings håndteres via miljøvariabler eller secrets manager i produksjon.         |
| Sårbarhetsskanning            | GitHub Dependabot og Snyk integreres i CI/CD-pipelinen for overvåking av avhengigheter.                   |
| Statisk kodeanalyse           | SonarQube eller tilsvarende kan benyttes for løpende kodekvalitetsanalyse.                                |
| Secret scanning               | GitHub Secret Scanning er aktivert for å oppdage lekkede hemmeligheter.                                   |
| HTTPS enforcement             | Alle API-kall håndteres over HTTPS (UseHttpsRedirection aktivert).                                        |
| Logging                       | Applikasjonen logger kun nødvendige feil og aldri sensitive data. Loggstrøm overvåkes via Grafana/Sentry. |
| Penetrasjonstesting           | Egen penetration testing planlegges før produksjonssetting.                                               |
| Patch management              | Alle avhengigheter oppdateres jevnlig med Dependabot.                                                     |





## Prosjektstruktur

