using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.DTOs.LeaderboardDtos;

namespace ProjectLucy.Application.Leaderboard.Queries.GetMentorLeaderboard;

public sealed record GetMentorLeaderboardQuery(
    Guid ViewerId,
    string Period,
    int Page,
    int PageSize) : IRequest<Result<MentorLeaderboardResponseDto>>;
