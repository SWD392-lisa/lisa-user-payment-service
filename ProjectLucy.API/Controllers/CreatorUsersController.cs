using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectLucy.Application.Creator;
using ProjectLucy.Application.Creator.DTOs;

namespace ProjectLucy.API.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "CreatorAccess")]
public sealed class CreatorUsersController : ControllerBase
{
    private readonly CreatorUserService _users;

    public CreatorUsersController(CreatorUserService users)
    {
        _users = users;
    }

    [HttpGet]
    public Task<CreatorUsersPageDto> Search(
        [FromQuery] string? search,
        [FromQuery] string? roleCode,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => _users.SearchAsync(search, roleCode, isActive, page, pageSize, ct);

    [HttpGet("{userId:guid}")]
    public Task<CreatorUserDto> Get(Guid userId, CancellationToken ct = default)
        => _users.GetAsync(userId, ct);

    [HttpPatch("{userId:guid}/status")]
    public async Task<ActionResult<CreatorUserDto>> UpdateStatus(
        Guid userId,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken ct = default)
    {
        var actorValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(actorValue, out var actorUserId))
            return Unauthorized(new { message = "User identity is missing" });

        return Ok(await _users.UpdateStatusAsync(userId, actorUserId, request, ct));
    }
}
