using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SpacePortalBackEnd.Models;
using SpacePortalBackEnd.Models.Auth;

var builder = WebApplication.CreateBuilder(args);

// ----- EF Core -----
builder.Services.AddDbContext<MyContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ----- HttpClients -----
builder.Services.AddHttpClient("Donki", client =>
{
    client.BaseAddress = new Uri("https://api.nasa.gov/DONKI/");
});

// ----- CORS (dev-open; tighten for prod) -----
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", cors =>
        cors.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// ----- Controllers / API explorer -----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer(); // keep only once

// ----- JWT auth (validate inputs + zero clock skew) -----
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey) ||
    string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("Missing Jwt configuration. Ensure Jwt:Key, Jwt:Issuer, and Jwt:Audience are set.");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ManageUserRoles", p => p.RequireRole("Admin"));

    options.AddPolicy("SelfOrAdmin", p => p.RequireAssertion(ctx =>
        ctx.User.IsInRole("Admin")));
});

// If you’re using custom users without full Identity, this gives you hashing:
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// ----- Swagger with Bearer auth -----
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SpacePortal API", Version = "v1" });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    };

    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ----- Pipeline -----
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Explicit endpoint avoids white/blank Swagger page issues
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SpacePortal API v1");
    });
}

// If your launchSettings has only HTTP during dev, consider gating HTTPS redirection:
app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
