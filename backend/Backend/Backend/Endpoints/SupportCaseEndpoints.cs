using Backend.Models;
using Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Backend.Endpoints
{
    public static class SupportCaseEndpoints
    {
        public static void MapSupportCaseEndpoints(this WebApplication app)
        {
            // GET: /cases
            app.MapGet("/cases", async (HttpContext context, AppDbContext db, ILogger<Program> logger) =>
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = context.User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                {
                    return Results.Unauthorized();
                }

                List<SupportCase> cases;

                if (role == "User")
                {
                    cases = await db.SupportCases
                        .Where(c => c.CreatedById == userId)
                        .ToListAsync();
                }
                else if (role == "SupportStaff" || role == "Admin")
                {
                    cases = await db.SupportCases.ToListAsync();
                }
                else
                {
                    return Results.Forbid(); // ukjent rolle
                }

                logger.LogInformation("Bruker {userId} med rolle {role} hentet {count} saker", userId, role, cases.Count);
                return Results.Ok(cases);

            }).RequireAuthorization();


            // POST-endepunkt: oppretter ny support-sak
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


        }

    }

}
