
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

## Prosjektstruktur

