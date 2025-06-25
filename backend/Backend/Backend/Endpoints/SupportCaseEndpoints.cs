using Backend.Models;
using Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Backend.DTOs;



namespace Backend.Endpoints
{
    public static class SupportCaseEndpoints
    {
        public static void MapSupportCaseEndpoints(this WebApplication app)

        {


            // GET-endepunkt for å hente support-saker, med søk og filtrering

            app.MapGet("/cases", async (
                HttpContext context,
                AppDbContext db,
                ILogger<Program> logger,
                [FromQuery] string? search, // valgfritt søkeord i tittel eller beskrivelse
                [FromQuery] string? status  // valgfri filtrering på status
            ) =>

            {
                // Henter bruker-ID og rolle fra JWT-tokenet
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = context.User.FindFirst(ClaimTypes.Role)?.Value;


                // Hvis bruker ikke er logget inn, returner 401
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                    return Results.Unauthorized();

                // Start en spørring som kan bygges videre på
                var query = db.SupportCases.AsQueryable();

                // Hvis brukeren er en vanlig bruker, skal de bare se sine egne saker
                if (role == "User")
                {
                    query = query.Where(c => c.CreatedById == userId);
                }
                // Hvis rollen er ukjent eller ikke har tilgang, nekt tilgang
                else if (role != "SupportStaff" && role != "Admin")
                {
                    return Results.Forbid();
                }

                // Hvis status-filter er gitt, filtrer på det
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(c => c.Status.ToLower() == status.ToLower());
                }

                // Hvis søkeord er oppgitt, filtrer på tittel og beskrivelse
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowered = search.ToLower();
                    query = query.Where(c =>
                        c.Title.ToLower().Contains(lowered) ||
                        c.Description.ToLower().Contains(lowered));
                }

                // Henter resultatet fra databasen
                var cases = await query.ToListAsync();

                // Logger antall saker og hvem som hentet dem
                logger.LogInformation("Bruker {userId} med rolle {role} hentet {count} saker", userId, role, cases.Count);

                // Lagre handlingen i audit-loggen
                db.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    Role = role,
                    Action = "ViewedCases",
                    Timestamp = DateTime.UtcNow,
                    AdditionalInfo = $"Antall saker: {cases.Count}"
                });

                await db.SaveChangesAsync(); // Lagrer loggen

                // Returnerer listen over saker som JSON (200 OK)
                return Results.Ok(cases);
            }).RequireAuthorization(); // Kun for innloggede brukere







            // POST-endepunkt: API som brukes til å opprette ny supportsak
            app.MapPost("/cases", async (
                [FromBody] SupportCase supportCase,
                HttpContext httpContext, // brukes for å hente innlogget bruker fra JWT-token
                UserManager<ApplicationUser> userManager, // brukes for å slå opp bruker i databasen
                AppDbContext db) =>
            {

                var validationContext = new ValidationContext(supportCase); //lager et valideringskontekst-objekt for modellen supportCase. Dette er nødvendig fordi validatoren trenger litt "metadata" om hvilket objekt som skal valideres.
                var validationResults = new List<ValidationResult>(); // lager en tom liste som skal fylles med eventuelle feilmeldinger under valideringen


                // Kjører valideringen av modellen
                if (!Validator.TryValidateObject(supportCase, validationContext, validationResults, true))
                {
                    // Hvis valideringen feiler, returner 400 Bad Request med feilmeldingene
                    var errors = validationResults.Select(v => v.ErrorMessage); //samler opp feilmeldingene
                    return Results.BadRequest(errors); //returnerer feilene til klienten
                }


                // Henter bruker-ID fra tokenet (NameIdentifier er standard claim for bruker-ID)
                var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized(); // hvis vi ikke finner bruker-ID i tokenet, så er forespørselen ugyldig
                }


                // Henter brukeren fra databasen basert på ID
                var user = await userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    return Results.Unauthorized(); // hvis brukeren ikke finnes i databasen
                }

                // Setter opprettelsestidspunkt til nå (dette var fra før)
                supportCase.CreatedAt = DateTime.UtcNow;

                // Setter bruker-ID som opprettet saken (kobling til innlogget bruker)
                supportCase.CreatedById = user.Id;

                db.SupportCases.Add(supportCase); // Legger til support-saken i databasen
                await db.SaveChangesAsync(); // Lagrer endringene til databasen

                return Results.Created($"/cases/{supportCase.Id}", supportCase); // returnerer HTTP 201 Created med den lagrede saken


            }).RequireAuthorization(); // kun innloggede brukere får sende forespørsel



            // PATCH /cases/{id} er et API-endepunkt for å oppdatere status på en eksisterende supportsak

            app.MapPatch("/cases/{id}", async (
                int id, // ID-en til supportsaken vi ønsker å oppdatere
                [FromBody] UpdateCaseStatusRequest request, //ny status sendes inn i JSON-format
                HttpContext context, // brukes for å hente informasjon om den innloggede brukeren
                AppDbContext db, //tilgang til databasen
                ILogger<Program> logger // logger som brukes for feilsøking og historikk
            ) =>

            {
                //henter ID og rolle fra JWT-tokenet til den innloggede brukeren
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = context.User.FindFirst(ClaimTypes.Role)?.Value;


                // Hvis ikke bruker er logget inn eller mangler rolle så vil det føre til unauthorized
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                    return Results.Unauthorized();


                //bare Admin eller SupportStaff har tilgang til å endre status
                if (role != "Admin" && role != "SupportStaff")
                    return Results.Forbid();

                // finner supportsaken med en gitt ID
                var supportCase = await db.SupportCases.FindAsync(id);


                // Hvis den ikke finnes, så returneres 404 Not Found
                if (supportCase == null)
                    return Results.NotFound($"Support-sak med ID {id} ble ikke funnet.");


                var gammelStatus = supportCase.Status; // Ta vare på nåværende status før endring
                supportCase.Status = request.Status;   // Oppdaterer til ny status


                // logikk for når saken ble lukket
                if (request.Status == "Closed")
                {
                    supportCase.ClosedAt = DateTime.UtcNow;
                }
                else
                {
                    // Fjern lukketid dersom saken åpnes på nytt
                    supportCase.ClosedAt = null;
                }


                // lagrer endringene i databasen
                await db.SaveChangesAsync();


                // Opprett og lagre en auditlogg

                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Role = role,
                    Action = "Oppdaterte status",
                    CaseId = supportCase.Id,
                    Timestamp = DateTime.UtcNow,
                    Details = $"Endret status fra '{gammelStatus}' til '{request.Status}'"
                };


                db.AuditLogs.Add(auditLog);
                await db.SaveChangesAsync(); // Lagre auditloggen



                // Logger hvem som endret hva
                logger.LogInformation(
                    "Bruker {userId} (rolle: {role}) endret status på sak {caseId} fra '{gammelStatus}' til '{nyStatus}' kl {time}",
                     userId, role, supportCase.Id, gammelStatus, request.Status, DateTime.UtcNow

                );

                
                return Results.Ok(supportCase); // Returnerer den oppdaterte saken som respons



            }).RequireAuthorization(); // skal kun tilgjengelig for innloggede brukere




        }

    }

}
