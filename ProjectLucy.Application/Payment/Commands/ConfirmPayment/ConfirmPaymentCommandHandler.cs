using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.Interfaces;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;
using WalletEntity = ProjectLucy.Domain.Entities.Wallet;

namespace ProjectLucy.Application.Payment.Commands.ConfirmPayment;

public class ConfirmPaymentCommandHandler : IRequestHandler<ConfirmPaymentCommand, Result<object>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly string[] TerminalStatuses = ["completed", "failed", "cancelled"];

    public ConfirmPaymentCommandHandler(
        ITransactionRepository transactionRepo,
        IWalletRepository walletRepo,
        IUnitOfWork unitOfWork)
    {
        _transactionRepo = transactionRepo;
        _walletRepo = walletRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<object>> Handle(ConfirmPaymentCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;

        // 1. Look up the pending transaction
        var transaction = await _transactionRepo.GetByReferenceCodeAsync(req.OrderInvoiceNumber, ct);
        if (transaction is null)
            return Result<object>.Failure(404, $"Transaction with invoice '{req.OrderInvoiceNumber}' not found");

        // 2. Verify this transaction belongs to the authenticated user
        if (transaction.UserId != cmd.UserId)
            return Result<object>.Failure(403, "This transaction does not belong to the current user");

        // 3. Idempotency check
        var currentStatus = (transaction.Status ?? string.Empty).ToLowerInvariant();
        if (TerminalStatuses.Contains(currentStatus))
            return Result<object>.Success(new { received = true }, "Payment already processed (idempotent)");

        // 4. Map status — DB constraint only allows: pending, completed, failed, cancelled
        var newStatus = req.Status.ToLowerInvariant() switch
        {
            "success" => "completed",
            "failed" => "failed",
            "cancelled" => "cancelled",
            _ => transaction.Status
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
                Note = $"SePay deposit (frontend confirm): {transaction.ReferenceCode}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return Result<object>.Success(new { received = true }, "Payment confirmed");
    }
}
