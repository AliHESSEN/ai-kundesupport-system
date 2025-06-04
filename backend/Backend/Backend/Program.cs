
using Backend.Data; 
using Backend.Models; 
using Microsoft.AspNetCore.Mvc; 
using Microsoft.EntityFrameworkCore; 
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

// Lager en builder for å konfigurere appen
var builder = WebApplication.CreateBuilder(args); // Starter konfigurasjon av appen

// Registrerer databasen (DbContext) og setter opp tilkoblingen til PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))); //henter connection string fra appsettings.json


// Registrerer Identity-systemet som skal bruke databasen vår
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();


// Legger til støtte for Swagger (API-dokumentasjon)
builder.Services.AddEndpointsApiExplorer(); //legger til støtte for minimal API-dokumentasjon
builder.Services.AddSwaggerGen(); // legger til Swagger generator


// Bygger appen basert på konfigureringen over
var app = builder.Build();

// Viser Swagger kun i utviklingsmiljø
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); //aktiverer Swagger i dev
    app.UseSwaggerUI(); //aktiverer Swagger UI
}

// Tvinger HTTPS-omdirigering
app.UseHttpsRedirection(); // Omdirigerer automatisk HTTP til HTTPS

// Enkelt test-endepunkt som returnerer "pong" ved GET /ping
app.MapGet("/ping", () => Results.Ok("pong")); // helse-sjekk-endepunkt

// API-endepunkter

// GET-endepunkt: henter alle support-saker
app.MapGet("/cases", async (AppDbContext db) =>
{
    var cases = await db.SupportCases.ToListAsync(); // Leser alle support-saker fra databasen
    return Results.Ok(cases); // Returnerer listen som HTTP 200 OK
});

// POST-endepunkt: oppretter ny support-sak
app.MapPost("/cases", async ([FromBody] SupportCase supportCase, AppDbContext db) =>
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

    supportCase.CreatedAt = DateTime.UtcNow; //setter opprettelsestidspunkt til nå

    db.SupportCases.Add(supportCase); // Legger til support-saken i databasen
    await db.SaveChangesAsync(); // Lagrer endringene til databasen

    return Results.Created($"/cases/{supportCase.Id}", supportCase); // returnerer HTTP 201 Created med den lagrede saken
});

// Starter appen (webserveren)
app.Run(); 
