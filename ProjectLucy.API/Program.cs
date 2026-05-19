using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProjectLucy.BLL.IServices;
using ProjectLucy.BLL.Services;
using ProjectLucy.BLL.Settings;
using ProjectLucy.DAL.Base;
using ProjectLucy.DAL.Context;
using ProjectLucy.DAL.Entities;
using ProjectLucy.DAL.UnitOfWork;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────
// Database
// ─────────────────────────────────────────────────────────────
builder.Services.AddDbContext<NeondbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─────────────────────────────────────────────────────────────
// JWT Settings
// ─────────────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSection);
var jwtSettings = jwtSection.Get<JwtSettings>()!;

// ─────────────────────────────────────────────────────────────
// Authentication & Authorization
// ─────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero  // No tolerance for expired tokens
    };
});

builder.Services.AddAuthorization();

// ─────────────────────────────────────────────────────────────
// Repositories & Unit of Work (Scoped — one per HTTP request)
// ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IGenericRepositories<User>, GenericRepositories<User>>();
builder.Services.AddScoped<IGenericRepositories<Role>, GenericRepositories<Role>>();
builder.Services.AddScoped<IGenericRepositories<RefreshToken>, GenericRepositories<RefreshToken>>();

// ─────────────────────────────────────────────────────────────
// Business Logic Services
// ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ─────────────────────────────────────────────────────────────
// Controllers
// ─────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ─────────────────────────────────────────────────────────────
// Swagger with JWT support
// ─────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ProjectLucy API",
        Version = "v1"
    });

    // Add JWT Bearer input to Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Example: Bearer {your_token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ─────────────────────────────────────────────────────────────
// CORS (adjust origins for your FE dev server)
// ─────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",   // React / Next.js
                "http://localhost:5173",   // Vite
                "http://localhost:4200"    // Angular
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();           // Required for cookies
    });
});

var app = builder.Build();

// ─────────────────────────────────────────────────────────────
// Middleware pipeline
// ─────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
