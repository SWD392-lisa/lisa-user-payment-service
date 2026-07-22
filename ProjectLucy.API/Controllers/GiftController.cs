using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectLucy.Application.DTOs.GiftDtos;
using ProjectLucy.Application.Gift.Commands.SendGift;
using ProjectLucy.Application.Gift.Queries.GetActiveGifts;
using ProjectLucy.Application.Gift.Queries.GetRoomGiftTransactions;
using ProjectLucy.Infrastructure.Persistence;
using ProjectLucy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ProjectLucy.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GiftController : ControllerBase
{
    private readonly ISender _sender;
    private readonly NeonDbContext _db;

    public GiftController(ISender sender, NeonDbContext db)
    {
        _sender = sender;
        _db = db;
    }

    [HttpPut("room/{roomSessionId:guid}/recipient")]
    public async Task<IActionResult> RegisterRoomRecipient(Guid roomSessionId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var user = await _db.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (user == null || !new[] { "PRO", "SUPER" }.Contains(user.Role.RoleCode.ToUpperInvariant()))
            return Forbid();
        var mapping = await _db.RoomGiftRecipients.SingleOrDefaultAsync(x => x.RoomSessionId == roomSessionId, ct);
        if (mapping == null)
        {
            mapping = new RoomGiftRecipient
            {
                Id = Guid.NewGuid(), RoomSessionId = roomSessionId, RecipientUserId = userId,
                IsActive = true, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddHours(4)
            };
            _db.RoomGiftRecipients.Add(mapping);
        }
        else
        {
            mapping.RecipientUserId = userId;
            mapping.IsActive = true;
            mapping.ExpiresAt = DateTime.UtcNow.AddHours(4);
        }
        await _db.SaveChangesAsync(ct);
        return Ok(new { roomSessionId, registered = true, expiresAt = mapping.ExpiresAt });
    }

    [HttpDelete("room/{roomSessionId:guid}/recipient")]
    public async Task<IActionResult> DeactivateRoomRecipient(Guid roomSessionId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var mapping = await _db.RoomGiftRecipients.SingleOrDefaultAsync(
            x => x.RoomSessionId == roomSessionId && x.RecipientUserId == userId, ct);
        if (mapping == null) return NotFound();
        mapping.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("send-to-room")]
    public async Task<IActionResult> SendGiftToRoom([FromBody] SendGiftToRoomRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var senderId)) return Unauthorized();
        if (request.IdempotencyKey == Guid.Empty)
            return BadRequest(new { message = "idempotencyKey is required" });
        var recipient = await _db.RoomGiftRecipients.Include(x => x.Recipient).SingleOrDefaultAsync(
            x => x.RoomSessionId == request.RoomSessionId && x.IsActive && x.ExpiresAt > DateTime.UtcNow, ct);
        if (recipient == null) return BadRequest(new { message = "This room cannot receive gifts" });
        var persona = await _db.AnonymousRoomIdentities.AsNoTracking().SingleOrDefaultAsync(
            x => x.UserId == senderId && x.RoomSessionId == request.RoomSessionId && x.ExpiresAt > DateTime.UtcNow, ct);
        if (persona == null) return BadRequest(new { message = "Join the room with an anonymous Persona before sending a gift" });

        var result = await _sender.Send(new SendGiftCommand
        {
            SenderId = senderId,
            Request = new SendGiftRequest
            {
                GiftId = request.GiftId,
                ReceiverId = recipient.RecipientUserId,
                Quantity = request.Quantity,
                RoomSessionId = request.RoomSessionId,
                IdempotencyKey = request.IdempotencyKey
            }
        }, ct);
        if (!result.IsSuccess || result.Data == null) return StatusCode(result.Status, result);

        var exists = await _db.GiftEventOutboxes.AnyAsync(x => x.GiftTransactionId == result.Data.Id, ct);
        if (!exists)
        {
            var eventId = Guid.NewGuid();
            var payload = JsonSerializer.Serialize(new
            {
                eventId,
                sessionId = request.RoomSessionId,
                sender = new { anonymousId = persona.AnonymousId, displayName = persona.DisplayName, personaCode = persona.PersonaCode, personaAssetUrl = persona.PersonaAssetUrl },
                recipient = new { displayName = recipient.Recipient.UserFullName },
                gift = new { id = result.Data.GiftId, name = result.Data.GiftName, iconUrl = result.Data.GiftIconUrl },
                quantity = result.Data.Quantity,
                totalValue = result.Data.TotalValue,
                occurredAt = result.Data.CreatedAt
            });
            _db.GiftEventOutboxes.Add(new GiftEventOutbox
            {
                Id = eventId, GiftTransactionId = result.Data.Id, Payload = payload,
                Status = "PENDING", Attempts = 0, NextAttemptAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }
        return StatusCode(result.Status, result);
    }

    [HttpGet("catalog")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveGifts()
    {
        var result = await _sender.Send(new GetActiveGiftsQuery());
        return StatusCode(result.Status, result);
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendGift([FromBody] SendGiftRequest request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { Status = 401, Message = "Invalid or missing user identity" });

        var result = await _sender.Send(new SendGiftCommand
        {
            Request = request,
            SenderId = userId
        });
        return StatusCode(result.Status, result);
    }

    [HttpGet("room/{roomSessionId:guid}")]
    public async Task<IActionResult> GetRoomGiftTransactions(Guid roomSessionId)
    {
        var result = await _sender.Send(new GetRoomGiftTransactionsQuery { RoomSessionId = roomSessionId });
        return StatusCode(result.Status, result);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}
