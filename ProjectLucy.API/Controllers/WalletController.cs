using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectLucy.Application.Wallet.Queries.GetWalletBalance;

namespace ProjectLucy.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    private readonly ISender _sender;

    public WalletController(ISender sender)
    {
        _sender = sender;
    }

    // ─────────────────────────────────────────────────────────────
    // GET /api/wallet/balance
    // ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Return the authenticated user's wallet balance.
    /// Returns zero if no wallet exists yet.
    /// </summary>
    [HttpGet("balance")]
    [Authorize]
    public async Task<IActionResult> GetBalance()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { Status = 401, Message = "Invalid or missing user identity" });

        var result = await _sender.Send(new GetWalletBalanceQuery(userId));
        return StatusCode(result.Status, result);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}
