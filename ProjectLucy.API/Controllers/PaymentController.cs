using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectLucy.Application.Payment.Commands.CreatePayment;
using ProjectLucy.Application.Payment.Commands.ConfirmPayment;
using ProjectLucy.Application.Payment.Commands.HandleIpn;
using ProjectLucy.Application.Payment.Queries.GetPaymentHistory;
using ProjectLucy.Shared.Dtos.PaymentDtos;

namespace ProjectLucy.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentController(ISender sender)
    {
        _sender = sender;
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/payment/create
    // ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Build a signed SePay form payload for the frontend to submit to the gateway.
    /// Records a PENDING transaction tied to the authenticated user.
    /// </summary>
    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!TryGetUserId(out var userId))
            return Unauthorized(new { Status = 401, Message = "Invalid or missing user identity" });

        var result = await _sender.Send(new CreatePaymentCommand(request, userId));
        return StatusCode(result.Status, result);
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/payment/ipn
    // ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Webhook target for SePay. Verifies the signature, updates the local
    /// transaction record, and always returns 200 so SePay stops retrying.
    /// </summary>
    [HttpPost("ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> Ipn([FromBody] IpnRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _sender.Send(new HandleIpnCommand(request));
        return StatusCode(result.Status, result);
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/payment/confirm
    // ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Called by the frontend after SePay redirects the user back.
    /// Updates the transaction status and credits the wallet.
    /// This is a fallback for when the IPN webhook is not available
    /// (e.g. sandbox mode or backend not publicly accessible).
    /// </summary>
    [HttpPost("confirm")]
    [Authorize]
    public async Task<IActionResult> Confirm([FromBody] ConfirmPaymentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!TryGetUserId(out var userId))
            return Unauthorized(new { Status = 401, Message = "Invalid or missing user identity" });

        var result = await _sender.Send(new ConfirmPaymentCommand(request, userId));
        return StatusCode(result.Status, result);
    }

    // ─────────────────────────────────────────────────────────────
    // GET /api/payment/history
    // ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Return the authenticated user's payment history (newest first).
    /// </summary>
    [HttpGet("history")]
    [Authorize]
    public async Task<IActionResult> History()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { Status = 401, Message = "Invalid or missing user identity" });

        var result = await _sender.Send(new GetPaymentHistoryQuery(userId));
        return StatusCode(result.Status, result);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}
