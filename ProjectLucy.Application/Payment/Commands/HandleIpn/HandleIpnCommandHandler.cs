using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.Common.Exceptions;
using ProjectLucy.Application.Interfaces;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;
using WalletEntity = ProjectLucy.Domain.Entities.Wallet;

namespace ProjectLucy.Application.Payment.Commands.HandleIpn;

public class HandleIpnCommandHandler : IRequestHandler<HandleIpnCommand, Result<object>>
{
    private readonly ISePayService _sePayService;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly string[] TerminalStatuses = ["completed", "failed", "cancelled"];

    public HandleIpnCommandHandler(
        ISePayService sePayService,
        ITransactionRepository transactionRepo,
        IWalletRepository walletRepo,
        IUnitOfWork unitOfWork)
    {
        _sePayService = sePayService;
        _transactionRepo = transactionRepo;
        _walletRepo = walletRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<object>> Handle(HandleIpnCommand cmd, CancellationToken ct)
    {
        var ipn = cmd.Request;

        // 1. Verify signature — refuse anything that does not come from SePay
        var fields = new Dictionary<string, string>
        {
            ["order_invoice_number"] = ipn.OrderInvoiceNumber ?? string.Empty,
            ["transaction_status"]   = ipn.TransactionStatus ?? string.Empty,
            ["transaction_id"]       = ipn.TransactionId ?? string.Empty,
            ["order_amount"]         = ipn.OrderAmount ?? string.Empty,
            ["payment_method"]       = ipn.PaymentMethod ?? string.Empty,
        };

        if (!_sePayService.VerifyIpnSignature(fields, ipn.Signature ?? string.Empty))
            throw new ForbiddenException("Invalid IPN signature");

        // 2. Resolve the transaction created during /api/payment/create (tracked for update)
        var transaction = await _transactionRepo.GetByReferenceCodeAsync(ipn.OrderInvoiceNumber!, ct);
        if (transaction is null)
            throw new NotFoundException($"Transaction with invoice '{ipn.OrderInvoiceNumber}' was not found");

        // 3. Idempotency: a transaction already in a terminal state is not reprocessed
        var currentStatus = (transaction.Status ?? string.Empty).ToLowerInvariant();
        if (TerminalStatuses.Contains(currentStatus))
            return Result<object>.Success(new { received = true }, "IPN already processed (idempotent)");

        // 4. Map SePay status → internal status (DB constraint: pending, completed, failed, cancelled)
        var newStatus = ipn.TransactionStatus?.ToLowerInvariant() switch
        {
            "success"   => "completed",
            "failed"    => "failed",
            "cancelled" => "cancelled",
            _           => transaction.Status
        };
        transaction.Status = newStatus;
        transaction.UpdatedAt = DateTime.UtcNow;

        // 5. Credit wallet on successful payment
        if (newStatus == "completed" && transaction.Amount > 0)
        {
            var wallet = await _walletRepo.GetByUserIdAsync(transaction.UserId, ct);
            if (wallet is null)
            {
                wallet = new WalletEntity
                {
                    UserId = transaction.UserId,
                    Balance = 0,
                    Currency = "VND",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _walletRepo.AddAsync(wallet, ct);
            }

            wallet.Balance += transaction.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            wallet.WalletLedgers.Add(new WalletLedger
            {
                WalletId = wallet.Id,
                TransactionId = transaction.Id,
                Amount = transaction.Amount,
                BalanceAfter = wallet.Balance,
                EntryType = "DEPOSIT",
                Note = $"SePay deposit: {transaction.ReferenceCode}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return Result<object>.Success(new { received = true }, "IPN processed");
    }
}
