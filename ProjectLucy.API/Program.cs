using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProjectLucy.API.Middlewares;
using ProjectLucy.Application;
using ProjectLucy.Application.Settings;
using ProjectLucy.Infrastructure;
using System.Text;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Đọc file .env nếu tồn tại
var envPath = Path.Combine(builder.Environment.ContentRootPath, ".env");
if (File.Exists(envPath))
{
    foreach (var line in File.ReadAllLines(envPath))
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
        var parts = line.Split('=', 2);
        if (parts.Length == 2)
        {
            var key = parts[0].Trim();
            var val = parts[1].Trim();
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, val);
                builder.Configuration[key] = val;
            }
            else
            {
                builder.Configuration[key] = Environment.GetEnvironmentVariable(key);
            }
        }
    }
}

// Cấu hình động toàn bộ thông tin YARP cho Realtime NestJS Service từ biến môi trường
var realtimeUrl = builder.Configuration["REALTIME_SERVICE_URL"] ?? "http://localhost:3000/";
var authPolicy = builder.Configuration["REALTIME_SERVICE_AUTH_POLICY"] ?? "Default";
var clusterId = builder.Configuration["REALTIME_SERVICE_CLUSTER_ID"] ?? "realtime-cluster";
var agoraPath = builder.Configuration["REALTIME_SERVICE_AGORA_PATH"] ?? "/api/agora/{**catch-all}";
var socketioPath = builder.Configuration["REALTIME_SERVICE_SOCKETIO_PATH"] ?? "/socket.io/{**catch-all}";

builder.Configuration["ReverseProxy:Routes:agora-route:ClusterId"] = clusterId;
builder.Configuration["ReverseProxy:Routes:agora-route:AuthorizationPolicy"] = authPolicy;
builder.Configuration["ReverseProxy:Routes:agora-route:Match:Path"] = agoraPath;

builder.Configuration["ReverseProxy:Routes:socketio-route:ClusterId"] = clusterId;
builder.Configuration["ReverseProxy:Routes:socketio-route:AuthorizationPolicy"] = authPolicy;
builder.Configuration["ReverseProxy:Routes:socketio-route:Match:Path"] = socketioPath;

builder.Configuration["ReverseProxy:Clusters:realtime-cluster:Destinations:nest-realtime-instance:Address"] = realtimeUrl;

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSection);
var jwtSettings = jwtSection.Get<JwtSettings>()!;

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// Đăng ký YARP Reverse Proxy với các Transforms chuyển tiếp Header từ JWT Claims
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(transformContext =>
        {
            var user = transformContext.HttpContext.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userName = user.FindFirst(System.Security.Claims.ClaimTypes.Name ?? System.Security.Claims.ClaimTypes.GivenName)?.Value;
                var userEmail = user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var userRole = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                if (!string.IsNullOrEmpty(userId)) transformContext.ProxyRequest.Headers.Add("X-User-Id", userId);
                if (!string.IsNullOrEmpty(userName)) transformContext.ProxyRequest.Headers.Add("X-User-Name", userName);
                if (!string.IsNullOrEmpty(userEmail)) transformContext.ProxyRequest.Headers.Add("X-User-Email", userEmail);
                if (!string.IsNullOrEmpty(userRole)) transformContext.ProxyRequest.Headers.Add("X-User-Role", userRole);
            }
            return ValueTask.CompletedTask;
        });
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ProjectLucy API",
        Version = "v1"
    });

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Kích hoạt YARP Reverse Proxy middleware
app.MapReverseProxy();

app.MapControllers();

app.Run();

// Công khai class Program cho dự án test có thể truy cập qua WebApplicationFactory
public partial class Program { }
