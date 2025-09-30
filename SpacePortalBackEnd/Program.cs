using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SpacePortalBackEnd.Models;

var builder = WebApplication.CreateBuilder(args);

// ----- External config -----
var nasaKey = builder.Configuration["Nasa:ApiKey"]; // (still available if you need it)
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SpacePortal";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SpacePortalClients";

// ----- HttpClients -----
builder.Services.AddHttpClient("Donki", client =>
{
    client.BaseAddress = new Uri("https://api.nasa.gov/DONKI/");
});

// ----- EF Core -----
builder.Services.AddDbContext<MyContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ----- Controllers / Endpoints -----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ----- Swagger (with JWT 'Authorize' button) -----
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SpacePortal API",
        Version = "v1",
        Description = "SpacePortal backend with JWT auth (email-only login)"
    });

    // Bearer token support in Swagger UI
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Paste your JWT here (without quotes). Example: Bearer eyJhbGciOiJIUzI1NiIs...",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };
    c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});

// ----- CORS -----
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontEnd", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ----- JWT Auth -----
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = false; // set true behind HTTPS in prod
        o.SaveToken = true;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30), // small grace period
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

// If you created a JwtTokenService (from my previous message), register it here:
// builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

var app = builder.Build();

// ----- Swagger UI -----
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SpacePortal API v1");
        c.DocumentTitle = "SpacePortal API Docs";
        c.DisplayRequestDuration();
    });
}

// ----- Pipeline -----
app.UseHttpsRedirection();
app.UseCors("AllowFrontEnd");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Simple protected endpoint to test in Swagger quickly
app.MapGet("/api/secure/ping", () => Results.Ok(new { ok = true, atUtc = DateTime.UtcNow }))
   .RequireAuthorization();

app.Run();
