using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectLucy.Application.DTOs.PrivacyDtos;
using ProjectLucy.Application.Interfaces;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Infrastructure.Persistence;

namespace ProjectLucy.API.Controllers;

[ApiController]
[Route("api/privacy")]
[Authorize]
public class PrivacyController : ControllerBase
{
    private static readonly (string Code, string Label)[] Personas =
    {
        ("fox", "Cáo"), ("owl", "Cú"), ("panda", "Gấu Trúc"), ("rabbit", "Thỏ"),
        ("tiger", "Hổ"), ("koala", "Koala"), ("penguin", "Chim Cánh Cụt"), ("cat", "Mèo")
    };
    private static readonly string[] Colors = { "Xanh", "Vàng", "Cam", "Tím", "Bạc", "Ngọc", "Mây", "Nắng" };

    private readonly NeonDbContext _db;
    private readonly IJwtTokenService _tokens;

    public PrivacyController(NeonDbContext db, IJwtTokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    [HttpPost("room-persona")]
    public async Task<ActionResult<RoomPersonaResponse>> CreateRoomPersona(
        [FromBody] CreateRoomPersonaRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var user = await _db.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (user == null || !user.IsActive) return Unauthorized();
        if (!string.Equals(user.Role.RoleCode, "LUCY", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var identity = await _db.AnonymousRoomIdentities.SingleOrDefaultAsync(
            x => x.UserId == userId && x.RoomSessionId == request.RoomSessionId, ct);
        if (identity == null)
        {
            var persona = Personas[RandomNumberGenerator.GetInt32(Personas.Length)];
            var color = Colors[RandomNumberGenerator.GetInt32(Colors.Length)];
            identity = new AnonymousRoomIdentity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RoomSessionId = request.RoomSessionId,
                AnonymousId = Guid.NewGuid(),
                PersonaCode = persona.Code,
                PersonaAssetUrl = $"/personas/{persona.Code}.svg",
                DisplayName = $"{persona.Label} {color} {RandomNumberGenerator.GetInt32(100, 1000)}",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(4)
            };
            _db.AnonymousRoomIdentities.Add(identity);
            await _db.SaveChangesAsync(ct);
        }
        else if (identity.ExpiresAt <= DateTime.UtcNow)
        {
            identity.ExpiresAt = DateTime.UtcNow.AddHours(4);
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new RoomPersonaResponse
        {
            RoomSessionId = identity.RoomSessionId,
            AnonymousId = identity.AnonymousId,
            DisplayName = identity.DisplayName,
            PersonaCode = identity.PersonaCode,
            PersonaAssetUrl = identity.PersonaAssetUrl,
            RoomAccessToken = _tokens.GenerateRoomAccessToken(identity),
            ExpiresAt = identity.ExpiresAt
        });
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}
