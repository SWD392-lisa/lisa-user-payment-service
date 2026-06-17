using MediatR;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<ConfirmPaymentCommandHandler> _logger;

    private static readonly string[] TerminalStatuses = ["completed", "failed", "cancelled"];

    public ConfirmPaymentCommandHandler(
        ITransactionRepository transactionRepo,
        IWalletRepository walletRepo,
        IUnitOfWork unitOfWork,
        ILogger<ConfirmPaymentCommandHandler> logger)
    {
        _transactionRepo = transactionRepo;
        _walletRepo = walletRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<object>> Handle(ConfirmPaymentCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        _logger.LogInformation(
            "ConfirmPayment START — UserId={UserId}, Invoice={Invoice}, Status={Status}, Amount={Amount}, TxnId={TransactionId}",
            cmd.UserId, req.OrderInvoiceNumber, req.Status, req.Amount, req.TransactionId);

        // 1. Look up the pending transaction
        var transaction = await _transactionRepo.GetByReferenceCodeAsync(req.OrderInvoiceNumber, ct);
        if (transaction is null)
        {
            _logger.LogWarning("ConfirmPayment FAIL — Transaction not found for Invoice={Invoice}", req.OrderInvoiceNumber);
            return Result<object>.Failure(404, $"Transaction with invoice '{req.OrderInvoiceNumber}' not found");
        }
        _logger.LogInformation("ConfirmPayment — Found transaction Id={TxnId}, CurrentStatus={Status}, UserId={TxnUserId}, Amount={Amount}",
            transaction.Id, transaction.Status, transaction.UserId, transaction.Amount);

        // 2. Verify this transaction belongs to the authenticated user
        if (transaction.UserId != cmd.UserId)
        {
            _logger.LogWarning("ConfirmPayment FAIL — UserId mismatch: request UserId={ReqUserId}, transaction UserId={TxnUserId}",
                cmd.UserId, transaction.UserId);
            return Result<object>.Failure(403, "This transaction does not belong to the current user");
        }

        // 3. Idempotency check
        var currentStatus = (transaction.Status ?? string.Empty).ToLowerInvariant();
        if (TerminalStatuses.Contains(currentStatus))
        {
            _logger.LogInformation("ConfirmPayment IDEMPOTENT — Transaction {TxnId} already in terminal status {Status}", transaction.Id, currentStatus);
            return Result<object>.Success(new { received = true }, "Payment already processed (idempotent)");
        }

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
        _logger.LogInformation("ConfirmPayment — Status mapped: {OldStatus} → {NewStatus}", currentStatus, newStatus);

        // 5. Credit wallet on successful payment
        if (newStatus == "completed" && transaction.Amount > 0)
        {
            _logger.LogInformation("ConfirmPayment — Crediting wallet for UserId={UserId}, Amount={Amount}", transaction.UserId, transaction.Amount);

            var wallet = await _walletRepo.GetByUserIdAsync(transaction.UserId, ct);
            if (wallet is null)
            {
                _logger.LogInformation("ConfirmPayment — No existing wallet for UserId={UserId}, creating new one", transaction.UserId);
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
            else
            {
                _logger.LogInformation("ConfirmPayment — Existing wallet found: Id={WalletId}, BalanceBefore={Balance}", wallet.Id, wallet.Balance);
            }

            var balanceBefore = wallet.Balance;
            wallet.Balance += transaction.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var ledgerEntry = new WalletLedger
            {
                WalletId = wallet.Id,
                TransactionId = transaction.Id,
                Amount = transaction.Amount,
                BalanceAfter = wallet.Balance,
                EntryType = "DEPOSIT",
                Note = $"SePay deposit (frontend confirm): {transaction.ReferenceCode}",
                CreatedAt = DateTime.UtcNow
            };
            wallet.WalletLedgers.Add(ledgerEntry);

            _logger.LogInformation(
                "ConfirmPayment — Ledger entry prepared: WalletId={WalletId}, EntryType={EntryType}, Amount={Amount}, BalanceBefore={BalanceBefore}, BalanceAfter={BalanceAfter}",
                wallet.Id, ledgerEntry.EntryType, ledgerEntry.Amount, balanceBefore, ledgerEntry.BalanceAfter);
        }
        else if (newStatus != "completed")
        {
            _logger.LogInformation("ConfirmPayment — No wallet update needed: new status is {Status}", newStatus);
        }

        _logger.LogInformation("ConfirmPayment — Calling SaveChangesAsync for Invoice={Invoice}", req.OrderInvoiceNumber);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("ConfirmPayment SUCCESS — Invoice={Invoice}, NewStatus={Status}", req.OrderInvoiceNumber, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConfirmPayment FAILED — SaveChangesAsync threw for Invoice={Invoice}, Status={Status}, Error={ErrorType}",
                req.OrderInvoiceNumber, newStatus, ex.GetType().Name);
            throw;
        }

        return Result<object>.Success(new { received = true }, "Payment confirmed");
    }
}
