using Backend.Models;
using Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
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
                // les bruker-id fra flere mulige claim-navn
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? context.User.FindFirst("sub")?.Value; // fallback hvis token bruker "sub"

                // les rolle fra både ClaimTypes.Role og "role"
                var role = context.User.FindFirst(ClaimTypes.Role)?.Value
                        ?? context.User.FindFirst("role")?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                    return Results.Unauthorized();

                var query = db.SupportCases.AsQueryable();

                if (role == "User")
                {
                    query = query.Where(c => c.CreatedById == userId);
                }
                else if (role != "SupportStaff" && role != "Admin")
                {
                    return Results.Forbid();
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(c => c.Status.ToLower() == status.ToLower());
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowered = search.ToLower();
                    query = query.Where(c =>
                        c.Title.ToLower().Contains(lowered) ||
                        c.Description.ToLower().Contains(lowered));
                }

                var cases = await query.ToListAsync();

                logger.LogInformation("Bruker {userId} med rolle {role} hentet {count} saker", userId, role, cases.Count);

                db.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    Role = role,
                    Action = "ViewedCases",
                    Timestamp = DateTime.UtcNow,
                    AdditionalInfo = $"Antall saker: {cases.Count}"
                });

                await db.SaveChangesAsync();

                return Results.Ok(cases);
            }).RequireAuthorization(); // Kun for innloggede brukere

            // POST-endepunkt: opprette ny supportsak
            app.MapPost("/cases", async (
                [FromBody] SupportCase supportCase,
                HttpContext httpContext, // JWT-bruker
                UserManager<ApplicationUser> userManager,
                AppDbContext db) =>
            {
                var validationContext = new ValidationContext(supportCase);
                var validationResults = new List<ValidationResult>();

                if (!Validator.TryValidateObject(supportCase, validationContext, validationResults, true))
                {
                    var errors = validationResults.Select(v => v.ErrorMessage);
                    return Results.BadRequest(errors);
                }

                var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? httpContext.User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Results.Unauthorized();
                }

                supportCase.CreatedAt = DateTime.UtcNow;
                supportCase.CreatedById = user.Id;

                db.SupportCases.Add(supportCase);
                await db.SaveChangesAsync();

                return Results.Created($"/cases/{supportCase.Id}", supportCase);
            }).RequireAuthorization(); // kun innloggede brukere

            // PATCH: oppdater status
            app.MapPatch("/cases/{id}", async (
                int id,
                [FromBody] UpdateCaseStatusRequest request,
                HttpContext context,
                AppDbContext db,
                ILogger<Program> logger
            ) =>
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? context.User.FindFirst("sub")?.Value;

                var role = context.User.FindFirst(ClaimTypes.Role)?.Value
                        ?? context.User.FindFirst("role")?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                    return Results.Unauthorized();

                if (role != "Admin" && role != "SupportStaff")
                    return Results.Forbid();

                var supportCase = await db.SupportCases.FindAsync(id);
                if (supportCase == null)
                    return Results.NotFound($"Support-sak med ID {id} ble ikke funnet.");

                var gammelStatus = supportCase.Status;
                supportCase.Status = request.Status;

                if (request.Status == "Closed")
                {
                    supportCase.ClosedAt = DateTime.UtcNow;
                }
                else
                {
                    supportCase.ClosedAt = null;
                }

                await db.SaveChangesAsync();

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
                await db.SaveChangesAsync();

                logger.LogInformation(
                    "Bruker {userId} (rolle: {role}) endret status på sak {caseId} fra '{gammelStatus}' til '{nyStatus}' kl {time}",
                     userId, role, supportCase.Id, gammelStatus, request.Status, DateTime.UtcNow
                );

                return Results.Ok(supportCase);
            }).RequireAuthorization(); // kun innloggede
        }
    }
}
