using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using ProjectLucy.Gateway.Configuration;
using ProjectLucy.Gateway.Middlewares;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Config routing (không secret) commit trong repo. Thêm sớm để .env + override địa chỉ bên dưới thắng.
builder.Configuration.AddJsonFile("gateway-config.json", optional: false, reloadOnChange: true);

// Đọc file .env nếu tồn tại (cùng cách với ProjectLucy.API)
var envPath = Path.Combine(builder.Environment.ContentRootPath, ".env");
if (File.Exists(envPath))
{
    foreach (var line in File.ReadAllLines(envPath))
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
        var parts = line.Split('=', 2);
        if (parts.Length != 2) continue;

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

// Địa chỉ downstream override từ biến môi trường (mặc định: localhost cho dev).
// Trong Docker, compose set các biến này trỏ tới container name.
var apiUrl = builder.Configuration["API_SERVICE_URL"] ?? "http://localhost:5080/";
var curriculumUrl = builder.Configuration["CURRICULUM_SERVICE_URL"] ?? "http://localhost:8080/";
var repoUrl = builder.Configuration["REPO_SERVICE_URL"] ?? "http://localhost:8082/";
var realtimeUrl = builder.Configuration["REALTIME_SERVICE_URL"] ?? "http://localhost:3000/";

builder.Configuration["ReverseProxy:Clusters:api-cluster:Destinations:api-1:Address"] = apiUrl;
builder.Configuration["ReverseProxy:Clusters:curriculum-cluster:Destinations:curriculum-1:Address"] = curriculumUrl;
builder.Configuration["ReverseProxy:Clusters:repo-cluster:Destinations:repo-1:Address"] = repoUrl;
builder.Configuration["ReverseProxy:Clusters:realtime-cluster:Destinations:realtime-1:Address"] = realtimeUrl;

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ---- JWT (cùng secret/issuer/audience với ProjectLucy.API để token dùng chung) ----
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

// Policy "authenticated" — chỉ dùng cho repo-route (repo-service chưa có auth riêng).
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("authenticated", policy => policy.RequireAuthenticatedUser());
});

// ---- Rate limiting: fixed-window, có sẵn trong .NET 8, không thêm package ----
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("fixed", o =>
    {
        o.PermitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 100);
        o.Window = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:WindowSeconds", 60));
        o.QueueLimit = 0;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

// ---- Logging tập trung mỗi request đi qua gateway ----
builder.Services.AddHttpLogging(o =>
{
    o.LoggingFields = HttpLoggingFields.RequestMethod
        | HttpLoggingFields.RequestPath
        | HttpLoggingFields.ResponseStatusCode;
});

// ---- YARP Reverse Proxy + transforms ----
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        // Chống spoof: xóa mọi header identity/correlation client tự gửi, gateway tự set lại.
        builderContext.AddRequestHeaderRemove("X-User-Id");
        builderContext.AddRequestHeaderRemove("X-User-Name");
        builderContext.AddRequestHeaderRemove("X-User-Email");
        builderContext.AddRequestHeaderRemove("X-User-Role");
        builderContext.AddRequestHeaderRemove(CorrelationIdMiddleware.HeaderName);

        builderContext.AddRequestTransform(transformContext =>
        {
            var user = transformContext.HttpContext.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = user.FindFirst(ClaimTypes.Name)?.Value;
                var userEmail = user.FindFirst(ClaimTypes.Email)?.Value;
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

                if (!string.IsNullOrEmpty(userId)) transformContext.ProxyRequest.Headers.Add("X-User-Id", userId);
                if (!string.IsNullOrEmpty(userName)) transformContext.ProxyRequest.Headers.Add("X-User-Name", userName);
                if (!string.IsNullOrEmpty(userEmail)) transformContext.ProxyRequest.Headers.Add("X-User-Email", userEmail);
                if (!string.IsNullOrEmpty(userRole)) transformContext.ProxyRequest.Headers.Add("X-User-Role", userRole);
            }

            var correlationId = transformContext.HttpContext.Items[CorrelationIdMiddleware.ItemKey]?.ToString();
            if (!string.IsNullOrEmpty(correlationId))
                transformContext.ProxyRequest.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);

            return ValueTask.CompletedTask;
        });
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration["ALLOWED_ORIGINS"]?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            ?? new[]
            {
                "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:4200",
                "https://lisa-frontend-app.vercel.app"
            };

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseHttpLogging();
app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();
app.MapGet("/health", () => Results.Ok(new { service = "projectlucy-gateway", status = "ok" }));

app.Run();

// Công khai Program cho dự án test truy cập qua WebApplicationFactory
public partial class Program { }
