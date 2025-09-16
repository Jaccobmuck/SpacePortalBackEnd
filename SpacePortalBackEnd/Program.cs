using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using SpacePortalBackEnd.Models;

var builder = WebApplication.CreateBuilder(args);

var nasaKey = builder.Configuration["Nasa:ApiKey"]; // Get the API key from appsettings.json

builder.Services.AddHttpClient("Donki", client =>
{
    client.BaseAddress = new Uri("https://api.nasa.gov/DONKI/");
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontEnd",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000") // front end url
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});
builder.Services.AddDbContext<MyContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// builder.Services.AddScoped<DonkiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontEnd");

app.UseAuthorization();

app.MapControllers();

app.Run();
