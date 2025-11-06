using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Backend.Helpers; // legger til JWT helper
using Backend.Services; // AuthService
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Backend.Initialization;
using System.Security.Claims;
using Backend.Endpoints;
using System.IdentityModel.Tokens.Jwt;
using System.Linq; // <-- VIKTIG for LINQ

var builder = WebApplication.CreateBuilder(args);

// Registrerer databasen (DbContext) – bruker riktig database avhengig av miljø
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Environment.IsEnvironment("Testing"))
    {
        var testConn = builder.Configuration.GetConnectionString("TestConnection")
                        ?? "Data Source=TestDb.sqlite";
        options.UseSqlite(testConn);
    }
    else
    {
        var pgConn = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(pgConn);
    }
});

// Registrerer Identity-systemet som skal bruke databasen vår
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Egendefinerte tjenester
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<AuthService>();

// Slå av claim-mapping (behold "sub", "role" osv.)
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Leser JWT-innstillinger
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new Exception("SecretKey mangler");
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];

// Setter opp JWT-autentisering
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // HTTP er lov i testmiljø
    options.RequireHttpsMetadata = !builder.Environment.IsEnvironment("Testing");
    options.SaveToken = true;

    var keyBytes = Encoding.UTF8.GetBytes(secretKey);

    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Signatur
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

        // I testmiljø: ikke krev issuer/audience
        ValidateIssuer = !builder.Environment.IsEnvironment("Testing"),
        ValidateAudience = !builder.Environment.IsEnvironment("Testing"),

        // Utløpstid sjekkes
        ValidateLifetime = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        ClockSkew = TimeSpan.Zero,

        // VIKTIG: matcher tokenet som har "sub" og "role"
        NameClaimType = "sub",
        RoleClaimType = "role"
    };

    // Logging av JWT-feil i testmodus
    if (builder.Environment.IsEnvironment("Testing"))
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var hdr = ctx.Request.Headers["Authorization"].ToString();
                Console.WriteLine($"[JWT] Authorization-header: {hdr}");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"[JWT] Auth failed: {ctx.Exception.GetType().Name} - {ctx.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    }
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Skriv inn token: Bearer {token}"
    });

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

// Autorisasjon
builder.Services.AddAuthorization();

var app = builder.Build();

// Migrering og seed roller ved oppstart
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync(); // alltid migrering
    await DataInitializer.SeedRolesAsync(services); // seed roller
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/ping", () => Results.Ok("pong"));

// whoami kun i testing – returnerer claims
if (app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/whoami", (HttpContext ctx) =>
    {
        var auth = ctx.User?.Identity?.IsAuthenticated ?? false;

        // Gjør begge sider av ?? til samme type (List<object>)
        var claims = ctx.User?.Claims
            .Select(c => (object)new { c.Type, c.Value })
            .ToList()
            ?? new List<object>();

        return Results.Ok(new { authenticated = auth, claims });
    }).RequireAuthorization();
}

// Controllers + endpoints
app.MapControllers();
app.MapSupportCaseEndpoints();
app.MapAdminEndpoints();

app.Run();

public partial class Program { }
