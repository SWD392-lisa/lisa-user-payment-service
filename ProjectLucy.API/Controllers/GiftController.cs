using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectLucy.Application.DTOs.GiftDtos;
using ProjectLucy.Application.Gift.Commands.SendGift;
using ProjectLucy.Application.Gift.Queries.GetActiveGifts;
using ProjectLucy.Application.Gift.Queries.GetRoomGiftTransactions;

namespace ProjectLucy.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GiftController : ControllerBase
{
    private readonly ISender _sender;

    public GiftController(ISender sender)
    {
        _sender = sender;
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
