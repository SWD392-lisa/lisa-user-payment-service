using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectLucy.Application.RoleUpgrade.Queries.GetUpgradePackages;
using ProjectLucy.Application.RoleUpgrade.Commands.UpgradeUsingWallet;
using ProjectLucy.Application.RoleUpgrade.Commands.CreateUpgradePayment;
using ProjectLucy.Application.RoleUpgrade.Commands.ConfirmUpgradePayment;
using ProjectLucy.Application.RoleUpgrade.Commands.HandleUpgradeIpn;
using ProjectLucy.Application.DTOs.RoleUpgradeDtos;
using ProjectLucy.Application.DTOs.ConfirmPaymentDtos;
using ProjectLucy.Application.DTOs.PaymentDtos;

namespace ProjectLucy.API.Controllers;

[ApiController]
[Route("api/role-upgrade")]
public class RoleUpgradeController : ControllerBase
{
    private readonly ISender _sender;

    public RoleUpgradeController(ISender sender)
    {
        _sender = sender;
    }

    // ─────────────────────────────────────────────────────────────
    // GET /api/role-upgrade/packages
    // ─────────────────────────────────────────────────────────────
    [HttpGet("packages")]
    [Authorize]
    public async Task<IActionResult> GetPackages()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { Status = 401, Message = "Invalid or missing user identity" });

        var result = await _sender.Send(new GetUpgradePackagesQuery(userId));
        return StatusCode(result.Status, result);
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/role-upgrade/upgrade
    // ─────────────────────────────────────────────────────────────
    [HttpPost("upgrade")]
    [Authorize]
    public async Task<IActionResult> UpgradeUsingWallet([FromBody] UpgradeWalletRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!TryGetUserId(out var userId))
            return Unauthorized(new { Status = 401, Message = "Invalid or missing user identity" });

        var result = await _sender.Send(new UpgradeUsingWalletCommand(request, userId));
        return StatusCode(result.Status, result);
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/role-upgrade/create-payment
    // ─────────────────────────────────────────────────────────────
    [HttpPost("create-payment")]
    [Authorize]
    public async Task<IActionResult> CreatePayment([FromBody] CreateUpgradePaymentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!TryGetUserId(out var userId))
            return Unauthorized(new { Status = 401, Message = "Invalid or missing user identity" });

        var result = await _sender.Send(new CreateUpgradePaymentCommand(request, userId));
        return StatusCode(result.Status, result);
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/role-upgrade/confirm-payment
    // ─────────────────────────────────────────────────────────────
    [HttpPost("confirm-payment")]
    [Authorize]
    public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!TryGetUserId(out var userId))
            return Unauthorized(new { Status = 401, Message = "Invalid or missing user identity" });

        var result = await _sender.Send(new ConfirmUpgradePaymentCommand(request, userId));
        return StatusCode(result.Status, result);
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/role-upgrade/ipn
    // ─────────────────────────────────────────────────────────────
    [HttpPost("ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> Ipn([FromBody] IpnRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _sender.Send(new HandleUpgradeIpnCommand(request));
        return StatusCode(result.Status, result);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}
