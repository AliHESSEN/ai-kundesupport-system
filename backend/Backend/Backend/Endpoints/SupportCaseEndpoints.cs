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
            // GET: /cases  API-endepunkt som Henter supportsaker basert på hvem som er innlogget og hvilken rolle de har

            app.MapGet("/cases", async (HttpContext context, AppDbContext db, ILogger<Program> logger) =>
            {
                // får bruker-ID og rolle fra JWT-tokenet (som ble satt ved innlogging)
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = context.User.FindFirst(ClaimTypes.Role)?.Value;

                //dersom brukeren ikke er logget inn eller mangler rolleinformasjon, så return  401 Unauthorized
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                {
                    return Results.Unauthorized();
                }

                List<SupportCase> cases;

                // Hvis innlogget bruker er en vanlig bruker , så skal de kun se saker de selv har opprettet
                if (role == "User")
                {
                    cases = await db.SupportCases
                        .Where(c => c.CreatedById == userId)
                        .ToListAsync();
                }
                // Hvis innlogget bruker er SupportStaff eller Admin, så hent alle saker i systemet
                else if (role == "SupportStaff" || role == "Admin")
                {
                    cases = await db.SupportCases.ToListAsync();
                }
                // Hvis rollen ikke er kjent eller ikke har tilgang, returner 403 Forbid
                else
                {
                    return Results.Forbid();
                }

                // Logger at brukeren hentet saker, og hvor mange som ble hentet
                logger.LogInformation("Bruker {userId} med rolle {role} hentet {count} saker", userId, role, cases.Count);

                // Returnerer listen over saker med statuskode 200 OK
                return Results.Ok(cases);

            }).RequireAuthorization(); // bare innloggede brukere kan bruke dette endepunktet





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

                // Oppdaterer status på supportsaken med verdien fra klienten
                supportCase.Status = request.Status;

                // lagrer endringene i databasen
                await db.SaveChangesAsync();

                // logger hvem som endret hvilken sak til hvilken status
                logger.LogInformation("Bruker {userId} endret status på sak {caseId} til '{status}'", userId, id, request.Status);

                //returnerer den oppdaterte saken som bekreftelse
                return Results.Ok(supportCase);

            }).RequireAuthorization(); // skal kun tilgjengelig for innloggede brukere




        }

    }

}
