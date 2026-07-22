using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectLucy.Domain.Interfaces;
using ProjectLucy.Domain.Models;

namespace ProjectLucy.API.Tests;

public sealed class LeaderboardTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LeaderboardTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MentorLeaderboard_RequiresAuthenticationAndMentorRole()
    {
        using var factory = CreateFactory(new FakeLeaderboardRepository([]));
        var client = factory.CreateClient();

        var anonymous = await client.GetAsync("/api/leaderboard/mentors");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        Authenticate(client, Guid.NewGuid(), "1");
        var learner = await client.GetAsync("/api/leaderboard/mentors");
        Assert.Equal(HttpStatusCode.Forbidden, learner.StatusCode);
    }

    [Fact]
    public async Task MentorLeaderboard_ReturnsRankedGiftCountsWithoutFinancialValues()
    {
        var viewerId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var repository = new FakeLeaderboardRepository([
            Entry("00000000-0000-0000-0000-000000000001", "Lower value", 1_000, 50, 3),
            Entry(viewerId.ToString(), "Viewer mentor", 10_000, 3, 1),
            Entry("00000000-0000-0000-0000-000000000003", "Tie with fewer gifts", 10_000, 2, 2)
        ]);
        using var factory = CreateFactory(repository);
        var client = factory.CreateClient();
        Authenticate(client, viewerId, "2");

        var response = await client.GetAsync("/api/leaderboard/mentors?period=weekly&page=2&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("totalGiftValue", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("wallet", json, StringComparison.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(json);
        var data = document.RootElement.GetProperty("data");
        Assert.Equal(3, data.GetProperty("total").GetInt32());
        Assert.Equal(1, data.GetProperty("viewer").GetProperty("rank").GetInt32());
        Assert.Equal(3, data.GetProperty("viewer").GetProperty("giftCount").GetInt64());
        Assert.Equal(2, data.GetProperty("items")[0].GetProperty("rank").GetInt32());
    }

    [Fact]
    public async Task WeeklyLeaderboard_UsesMondayMidnightInBangkok()
    {
        var repository = new FakeLeaderboardRepository([]);
        var now = new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);
        using var factory = CreateFactory(repository, now);
        var client = factory.CreateClient();
        Authenticate(client, Guid.NewGuid(), "3");

        var response = await client.GetAsync("/api/leaderboard/mentors?period=weekly");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new DateTime(2026, 7, 19, 17, 0, 0, DateTimeKind.Utc), repository.LastPeriodStartUtc);
    }

    [Fact]
    public async Task MentorLeaderboard_ValidatesPeriodAndPagination()
    {
        using var factory = CreateFactory(new FakeLeaderboardRepository([]));
        var client = factory.CreateClient();
        Authenticate(client, Guid.NewGuid(), "2");

        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync("/api/leaderboard/mentors?period=monthly")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync("/api/leaderboard/mentors?pageSize=51")).StatusCode);
    }

    [Fact]
    public async Task AlltimeLeaderboard_UsesStableTieBreakAndCapsResultsAtTop100()
    {
        var entries = Enumerable.Range(1, 101)
            .Select(index => new MentorLeaderboardAggregate
            {
                MentorId = Guid.Parse($"00000000-0000-0000-0000-{index:D12}"),
                DisplayName = $"Mentor {index}",
                RoleCode = "SUPER",
                TotalGiftValue = 1_000,
                GiftCount = 5,
                LatestGiftAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)
            })
            .Reverse()
            .ToList();
        var viewerId = Guid.Parse("00000000-0000-0000-0000-000000000101");
        var repository = new FakeLeaderboardRepository(entries);
        using var factory = CreateFactory(repository);
        var client = factory.CreateClient();
        Authenticate(client, viewerId, "SUPER");

        var response = await client.GetAsync(
            "/api/leaderboard/mentors?period=alltime&page=2&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = document.RootElement.GetProperty("data");
        Assert.Equal(JsonValueKind.Null, data.GetProperty("periodStart").ValueKind);
        Assert.Equal(100, data.GetProperty("total").GetInt32());
        Assert.Equal(JsonValueKind.Null, data.GetProperty("viewer").ValueKind);
        Assert.Equal(50, data.GetProperty("items").GetArrayLength());
        Assert.Equal(51, data.GetProperty("items")[0].GetProperty("rank").GetInt32());
        Assert.Equal(
            "00000000-0000-0000-0000-000000000051",
            data.GetProperty("items")[0].GetProperty("mentorId").GetString());
        Assert.Null(repository.LastPeriodStartUtc);
    }

    private WebApplicationFactory<Program> CreateFactory(
        FakeLeaderboardRepository repository,
        DateTimeOffset? now = null)
        => _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMentorLeaderboardRepository>();
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<IMentorLeaderboardRepository>(repository);
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(now ?? DateTimeOffset.UtcNow));
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthScheme;
                options.DefaultForbidScheme = TestAuthHandler.AuthScheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthScheme, _ => { });
        }));

    private static MentorLeaderboardAggregate Entry(
        string mentorId,
        string name,
        decimal value,
        long gifts,
        int minutesAgo)
        => new()
        {
            MentorId = Guid.Parse(mentorId),
            DisplayName = name,
            RoleCode = "PRO",
            TotalGiftValue = value,
            GiftCount = gifts,
            LatestGiftAt = DateTime.UtcNow.AddMinutes(-minutesAgo)
        };

    private static void Authenticate(HttpClient client, Guid userId, string role)
    {
        client.DefaultRequestHeaders.Remove("X-Test-User-Id");
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
    }

    private sealed class FakeLeaderboardRepository : IMentorLeaderboardRepository
    {
        private readonly IReadOnlyList<MentorLeaderboardAggregate> _entries;

        public FakeLeaderboardRepository(IReadOnlyList<MentorLeaderboardAggregate> entries)
        {
            _entries = entries;
        }

        public DateTime? LastPeriodStartUtc { get; private set; }

        public Task<IReadOnlyList<MentorLeaderboardAggregate>> GetTopMentorsAsync(
            DateTime? periodStartUtc,
            int limit,
            CancellationToken ct = default)
        {
            LastPeriodStartUtc = periodStartUtc;
            return Task.FromResult(_entries);
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string AuthScheme = "LeaderboardTest";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Role", out var role))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var userId = Request.Headers["X-Test-User-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role.ToString())
            ], AuthScheme);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), AuthScheme)));
        }
    }
}
