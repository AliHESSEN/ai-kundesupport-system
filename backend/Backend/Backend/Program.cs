// Importerer AppDbContext og EF Core
using Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Backend.Models;

// Lager en builder for å konfigurere appen
var builder = WebApplication.CreateBuilder(args);

// Registrerer databasen (DbContext) og setter opp tilkoblingen til PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Legger til støtte for Swagger (API-dokumentasjon)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Bygger appen basert på konfigureringen over
var app = builder.Build();

// Viser Swagger kun i utviklingsmiljø
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Tvinger HTTPS-omdirigering
app.UseHttpsRedirection();

// Enkelt test-endepunkt som returnerer "pong" ved GET /ping
app.MapGet("/ping", () => Results.Ok("pong"));


// API-endepunkter

// GET-endepunkt: henter alle support-saker
app.MapGet("/cases", async (AppDbContext db) =>
{
    var cases = await db.SupportCases.ToListAsync();
    return Results.Ok(cases);
});

// POST-endepunkt: oppretter ny support-sak
app.MapPost("/cases", async ([FromBody] SupportCase supportCase, AppDbContext db) =>
{
    supportCase.CreatedAt = DateTime.UtcNow;
    db.SupportCases.Add(supportCase);
    await db.SaveChangesAsync();

    return Results.Created($"/cases/{supportCase.Id}", supportCase);
});


// Starter appen (webserveren)
app.Run();
