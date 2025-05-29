// Importerer AppDbContext og EF Core
using Backend.Data;
using Microsoft.EntityFrameworkCore;

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

// Starter appen (webserveren)
app.Run();
