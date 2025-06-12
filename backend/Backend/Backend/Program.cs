using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Backend.Helpers; // legger til helper for JWT
using Backend.Services; // legger til AuthService
using Microsoft.AspNetCore.Authentication.JwtBearer; // for JWT-autentisering
using Microsoft.IdentityModel.Tokens; // for token-validering
using System.Text; // for å konvertere hemmelig nøkkel til bytes
using Backend.Initialization;


// Lager en builder for å konfigurere appen
var builder = WebApplication.CreateBuilder(args); // Starter konfigurasjon av appen


// Registrerer databasen (DbContext) og setter opp tilkoblingen til PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))); //henter connection string fra appsettings.json


// Registrerer Identity-systemet som skal bruke databasen vår
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();


// Legger til våre egne tjenester i Dependency Injection
builder.Services.AddScoped<JwtHelper>(); // registrerer JwtHelper
builder.Services.AddScoped<AuthService>(); // registrerer AuthService


// Henter ut JWT-innstillinger fra appsettings.json
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];


// Konfigurerer JWT-autentisering
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, // sjekker hvem som utstedte tokenet
        ValidateAudience = true, // sjekker hvem som er publikum for tokenet
        ValidateLifetime = true, // sjekker at tokenet ikke er utløpt
        ValidateIssuerSigningKey = true, // sjekker at signaturen er korrekt

        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});



// Legger til støtte for kontrollerne våre (f.eks. AuthController)
builder.Services.AddControllers();



// Legger til støtte for Swagger (API-dokumentasjon)
builder.Services.AddEndpointsApiExplorer(); //legger til støtte for minimal API-dokumentasjon



builder.Services.AddSwaggerGen(options =>
{
    // Definerer hvordan Swagger skal håndtere JWT-token i UI
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization", // Navnet på header-feltet som brukes
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http, // Vi bruker HTTP-skjema
        Scheme = "Bearer", // Forteller at vi bruker Bearer tokens
        BearerFormat = "JWT", // Formatet er JWT
        In = Microsoft.OpenApi.Models.ParameterLocation.Header, // Token skal legges i HTTP header
        Description = "Skriv inn JWT-tokenet slik: Bearer {token}" // Bruksbeskrivelse i Swagger UI
    });



    // Sier til Swagger at alle endpoints kan sikres med denne Bearer-skjemaet
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer" // Henviser til definisjonen vi laget over
                }
            },
            Array.Empty<string>() // Ingen spesifikke scopes kreves
        }
    });
});





// Bygger appen basert på konfigureringen over
var app = builder.Build();


// dette lager admin, SupportStaff og User rollene i databasen hvis de ikke finnes fra før
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DataInitializer.SeedRolesAsync(services);
}




// Viser Swagger kun i utviklingsmiljø
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); //aktiverer Swagger i dev
    app.UseSwaggerUI(); //aktiverer Swagger UI
}



// Tvinger HTTPS-omdirigering
app.UseHttpsRedirection(); // Omdirigerer automatisk HTTP til HTTPS

// Legger til autentisering og autorisasjon i pipeline
app.UseAuthentication(); // aktiverer autentisering (JWT)
app.UseAuthorization(); // aktiverer autorisasjon



// Aktiverer kontrollerne våre
app.MapControllers(); // ruter alle kontrollerbaserte API-endepunkter



// Enkelt test-endepunkt som returnerer "pong" ved GET /ping
app.MapGet("/ping", () => Results.Ok("pong")); // helse-sjekk-endepunkt



// GET-endepunkt: henter alle support-saker
app.MapGet("/cases", async (AppDbContext db) =>
{
    var cases = await db.SupportCases.ToListAsync(); // Leser alle support-saker fra databasen
    return Results.Ok(cases); // Returnerer listen som HTTP 200 OK
}).RequireAuthorization(); // krever gyldig JWT-token



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



// Starter appen (webserveren)
app.Run();
