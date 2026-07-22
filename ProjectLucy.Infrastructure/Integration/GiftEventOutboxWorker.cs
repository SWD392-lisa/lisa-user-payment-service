using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectLucy.Infrastructure.Persistence;

namespace ProjectLucy.Infrastructure.Integration;

public class GiftEventOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IHttpClientFactory _clients;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GiftEventOutboxWorker> _logger;

    public GiftEventOutboxWorker(IServiceScopeFactory scopes, IHttpClientFactory clients, IConfiguration configuration, ILogger<GiftEventOutboxWorker> logger)
    {
        _scopes = scopes;
        _clients = clients;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (!stoppingToken.IsCancellationRequested)
        {
            await DispatchBatch(stoppingToken);
            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task DispatchBatch(CancellationToken ct)
    {
        var serviceUrl = (_configuration["REALTIME_SERVICE_URL"] ?? "http://localhost:3000/").TrimEnd('/');
        var internalKey = _configuration["INTERNAL_SERVICE_KEY"];
        if (string.IsNullOrWhiteSpace(internalKey)) return;

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NeonDbContext>();
        var events = await db.GiftEventOutboxes
            .Where(x => x.Status == "PENDING" && x.NextAttemptAt <= DateTime.UtcNow)
            .OrderBy(x => x.CreatedAt).Take(20).ToListAsync(ct);
        var client = _clients.CreateClient(nameof(GiftEventOutboxWorker));
        foreach (var item in events)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{serviceUrl}/api/internal/gift-events");
                request.Headers.Add("X-LUCY-INTERNAL-KEY", internalKey);
                request.Content = new StringContent(item.Payload, Encoding.UTF8, "application/json");
                using var response = await client.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();
                item.Status = "SENT";
                item.SentAt = DateTime.UtcNow;
                item.LastError = null;
            }
            catch (Exception ex)
            {
                item.Attempts += 1;
                item.LastError = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                item.NextAttemptAt = DateTime.UtcNow.AddSeconds(Math.Min(60, Math.Pow(2, Math.Min(item.Attempts, 6))));
                _logger.LogWarning("Gift event {EventId} delivery failed on attempt {Attempt}", item.Id, item.Attempts);
            }
        }
        if (events.Count > 0) await db.SaveChangesAsync(ct);
    }
}
