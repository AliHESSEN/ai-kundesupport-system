using Backend.Data;
using Backend.Models;
using Backend.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Backend.Endpoints
{
    public static class AdminEndpoints
    {
        // Utvider WebApplication med admin-endepunkter
        public static WebApplication MapAdminEndpoints(this WebApplication app)
        {
            // Opprett en gruppe for alle /admin-ruter, krever Admin-rolle
            var admin = app
                .MapGroup("/admin")
                .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

            // Definer GET /admin/dashboard
            admin.MapGet("/dashboard", async (
                    AppDbContext db,                  // DB-kontekst
                    ILogger<Program> logger,           // Logger for observability
                    ClaimsPrincipal user) =>          // Informasjon om innlogget bruker
            {
                // 1) Tell alle åpne saker (Status == "Open")
                var openCount = await db.SupportCases
                    .CountAsync(c => c.Status == "Open");

                // 2) Hent alle lukkede saker (Status == "Closed") med lukket-tid
                var closedCases = await db.SupportCases
                    .Where(c => c.Status == "Closed" && c.ClosedAt != null)
                    .ToListAsync();
                var closedCount = closedCases.Count;

                // 3) Beregn gjennomsnittlig løsningstid i timer
                double avgHours = 0;
                if (closedCount > 0)
                {
                    // TotalSeconds fra DateTime-differanse
                    var avgSeconds = closedCases
                        .Average(c => (c.ClosedAt!.Value - c.CreatedAt).TotalSeconds);
                    // Konverter til timer og rund av til 2 desimaler
                    avgHours = Math.Round(avgSeconds / 3600.0, 2);
                }

                // 4) Placeholder for antall agenter online
                var agentsOnline = 0;

                // Lag DTO med resultater
                var summary = new DashboardSummary
                {
                    OpenCases = openCount,
                    ClosedCases = closedCount,
                    AvgResolutionTimeHours = avgHours,
                    AgentsOnline = agentsOnline
                };

                // Logg hvem som hentet dashboardet og hvilke data som ble sendt
                logger.LogInformation(
                    "Admin {Email} hentet dashboard – {@Summary}",
                    user.FindFirstValue(ClaimTypes.Email),
                    summary);

                // Returner 200 OK med DTO
                return Results.Ok(summary);
            })
                .WithName("GetDashboardSummary")                 // Navn i Swagger
                .Produces<DashboardSummary>(StatusCodes.Status200OK)  // Dokumenter returtype 200
                .Produces(StatusCodes.Status403Forbidden);           // Dokumenter returtype 403

            return app;
        }
    }
}
