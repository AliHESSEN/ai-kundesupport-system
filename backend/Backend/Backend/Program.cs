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
using System.Security.Claims;
using Backend.Endpoints; // for MapSupportCaseEndpoints






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



// Enkelt test-endepunkt som returnerer "pong" ved GET /ping
app.MapGet("/ping", () => Results.Ok("pong")); // en test for endepunkt



// Starter appen (webserveren)
app.MapControllers(); // kontrollerbasert API
app.MapSupportCaseEndpoints(); // minimal API-er for support
app.MapAdminEndpoints(); // for adminDashbord endpoint
app.Run();





