namespace ProjectLucy.Application.Creator.DTOs;

public sealed class CreatorUsersPageDto
{
    public IReadOnlyList<CreatorUserDto> Items { get; init; } = Array.Empty<CreatorUserDto>();
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
