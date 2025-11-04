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

// Registrerer databasen (DbContext) – bruker riktig database avhengig av miljø
builder.Services.AddDbContext<AppDbContext>(options =>
{
    // Hvis appen kjører i "Testing" (settes av testene), bruk SQLite
    if (builder.Environment.IsEnvironment("Testing"))
    {
        var testConn = builder.Configuration.GetConnectionString("TestConnection")
                        ?? "Data Source=TestDb.sqlite";
        options.UseSqlite(testConn);
    }
    else
    {
        // Ellers (utvikling eller produksjon): bruk PostgreSQL
        var pgConn = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(pgConn);
    }
});

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey ?? string.Empty))
    };
});

// Legger til støtte for kontrollerne våre (f.eks. AuthController)
builder.Services.AddControllers();

// Legger til støtte for Swagger (API-dokumentasjon)
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // Definerer hvordan Swagger skal håndtere JWT-token i UI
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Skriv inn JWT-tokenet slik: Bearer {token}"
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
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Bygger appen basert på konfigureringen over
var app = builder.Build();

// Opprett/migrer DB før seeding – trygt skilt mellom Testing (SQLite) og andre miljø (PostgreSQL)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();

    if (app.Environment.IsEnvironment("Testing"))
    {
        // For integrasjonstester: start fra blank, isolert SQLite-fil
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        app.Logger.LogInformation("Database initialisert for TESTING med SQLite (EnsureDeleted + EnsureCreated).");
    }
    else
    {
        // I dev/prod: migrer PostgreSQL
        await db.Database.MigrateAsync();
        app.Logger.LogInformation("Database migrert (Migrate) for {EnvironmentName}.", app.Environment.EnvironmentName);
    }

    await DataInitializer.SeedRolesAsync(services);
}

// Viser Swagger kun i utviklingsmiljø
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Tvinger HTTPS-omdirigering
app.UseHttpsRedirection();

// Legger til autentisering og autorisasjon i pipeline
app.UseAuthentication();
app.UseAuthorization();

// Enkelt test-endepunkt som returnerer "pong" ved GET /ping
app.MapGet("/ping", () => Results.Ok("pong"));

// Starter appen (webserveren)
app.MapControllers();
app.MapSupportCaseEndpoints();
app.MapAdminEndpoints();
app.Run();
