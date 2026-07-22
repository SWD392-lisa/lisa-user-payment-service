using Microsoft.EntityFrameworkCore;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Infrastructure.Persistence;
using ProjectLucy.Infrastructure.Persistence.Repositories;

namespace ProjectLucy.API.Tests;

public sealed class MentorLeaderboardRepositoryTests
{
    [Fact]
    public async Task GetTopMentors_FiltersIneligibleRowsAndSumsCompletedGiftQuantities()
    {
        var options = new DbContextOptionsBuilder<NeonDbContext>()
            .UseInMemoryDatabase($"mentor-leaderboard-{Guid.NewGuid()}")
            .Options;
        await using var context = new TestNeonDbContext(options);

        var pro = User("00000000-0000-0000-0000-000000000001", "Active Pro", "PRO", true);
        var super = User("00000000-0000-0000-0000-000000000002", "Active Super", "SUPER", true);
        var learner = User("00000000-0000-0000-0000-000000000003", "Learner", "LEARNER", true);
        var inactive = User("00000000-0000-0000-0000-000000000004", "Inactive Pro", "PRO", false);
        var weekStart = new DateTime(2026, 7, 19, 17, 0, 0, DateTimeKind.Utc);

        context.GiftTransactions.AddRange(
            Gift(pro, 1, "completed", 2, 500, weekStart),
            Gift(pro, 2, "COMPLETED", 3, 700, weekStart.AddHours(1)),
            Gift(super, 3, "pending", 100, 50_000, weekStart.AddHours(2)),
            Gift(super, 4, "completed", 7, 900, weekStart.AddSeconds(-1)),
            Gift(learner, 5, "completed", 9, 10_000, weekStart.AddHours(2)),
            Gift(inactive, 6, "completed", 11, 20_000, weekStart.AddHours(2)));
        await context.SaveChangesAsync();

        var result = await new MentorLeaderboardRepository(context)
            .GetTopMentorsAsync(weekStart, 100);

        var mentor = Assert.Single(result);
        Assert.Equal(pro.UserId, mentor.MentorId);
        Assert.Equal(5, mentor.GiftCount);
        Assert.Equal(1_200, mentor.TotalGiftValue);
        Assert.Equal(weekStart.AddHours(1), mentor.LatestGiftAt);
    }

    private static User User(string id, string name, string roleCode, bool active)
        => new()
        {
            UserId = Guid.Parse(id),
            UserFullName = name,
            UserEmail = $"{id}@example.test",
            UserHashPassword = "test",
            UserBirthday = new DateOnly(2000, 1, 1),
            IsActive = active,
            RoleId = id[^1],
            Role = new Role { RoleId = id[^1], RoleCode = roleCode, RoleName = roleCode }
        };

    private static GiftTransaction Gift(
        User receiver,
        long transactionId,
        string status,
        int quantity,
        decimal value,
        DateTime createdAt)
        => new()
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            ReceiverId = receiver.UserId,
            Receiver = receiver,
            SenderId = receiver.UserId,
            Sender = receiver,
            GiftId = Guid.NewGuid(),
            Quantity = quantity,
            TotalValue = value,
            CreatedAt = createdAt,
            Transaction = new Transaction
            {
                Id = transactionId,
                UserId = receiver.UserId,
                User = receiver,
                TransactionTypeId = 1,
                Amount = value,
                Status = status
            }
        };

    private sealed class TestNeonDbContext : NeonDbContext
    {
        public TestNeonDbContext(DbContextOptions<NeonDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }
    }
}
