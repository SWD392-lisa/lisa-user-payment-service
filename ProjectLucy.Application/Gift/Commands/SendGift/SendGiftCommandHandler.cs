using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.Common.Exceptions;
using ProjectLucy.Application.DTOs.GiftDtos;
using ProjectLucy.Application.Interfaces;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Application.Gift.Commands.SendGift;

public class SendGiftCommandHandler : IRequestHandler<SendGiftCommand, Result<GiftTransactionDto>>
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IGiftCatalogRepository _giftCatalogRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly IGiftTransactionRepository _giftTxnRepo;
    private readonly IUnitOfWork _unitOfWork;

    public SendGiftCommandHandler(
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IGiftCatalogRepository giftCatalogRepo,
        ITransactionRepository transactionRepo,
        IWalletRepository walletRepo,
        IGiftTransactionRepository giftTxnRepo,
        IUnitOfWork unitOfWork)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _giftCatalogRepo = giftCatalogRepo;
        _transactionRepo = transactionRepo;
        _walletRepo = walletRepo;
        _giftTxnRepo = giftTxnRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<GiftTransactionDto>> Handle(SendGiftCommand cmd, CancellationToken ct)
    {
        if (cmd.Request.Quantity is < 1 or > 99)
            throw new BadRequestException("Gift quantity must be between 1 and 99");
        if (cmd.Request.IdempotencyKey.HasValue)
        {
            var previous = await _giftTxnRepo.GetByIdempotencyKeyAsync(cmd.Request.IdempotencyKey.Value, ct);
            if (previous != null)
                return Result<GiftTransactionDto>.Success(ToDto(previous), "Gift request already completed");
        }
        // 1. Validate sender exists and has LUCY role
        var sender = await _userRepo.GetByIdTrackedAsync(cmd.SenderId, ct);
        if (sender == null)
            throw new NotFoundException("Sender not found");

        var lucyRole = await _roleRepo.GetByCodeAsync("LUCY", ct);
        if (lucyRole == null)
            throw new ServerException("LUCY role not found in database");

        if (sender.RoleId != lucyRole.RoleId)
            throw new ForbiddenException("Only users with LUCY role can send gifts");

        // 2. Validate receiver exists
        var receiver = await _userRepo.GetByIdAsync(cmd.Request.ReceiverId, ct);
        if (receiver == null)
            throw new NotFoundException("Receiver not found");

        if (sender.UserId == receiver.UserId)
            throw new BadRequestException("You cannot send gifts to yourself");

        // 3. Validate gift exists and is active
        var gift = await _giftCatalogRepo.GetByIdAsync(cmd.Request.GiftId, ct);
        if (gift == null)
            throw new NotFoundException("Gift not found");

        if (!gift.IsActive)
            throw new BadRequestException("This gift is not available");

        // 4. Calculate total cost
        var totalCost = gift.Price * cmd.Request.Quantity;

        // 5. Validate sender's wallet has enough balance
        var senderWallet = await _walletRepo.GetByUserIdAsync(sender.UserId, ct);
        if (senderWallet == null)
            throw new BadRequestException("Sender does not have a wallet");

        if (senderWallet.Balance < totalCost)
            throw new BadRequestException("Insufficient balance in wallet");

        // 6. Resolve transaction types
        var giftSendTypeId = await _transactionRepo.GetTypeIdByCodeAsync("GIFT_SEND", ct);
        if (giftSendTypeId == null)
            throw new ServerException("Transaction type GIFT_SEND not found, please seed transaction types first");

        var giftReceiveTypeId = await _transactionRepo.GetTypeIdByCodeAsync("GIFT_RECEIVE", ct);
        if (giftReceiveTypeId == null)
            throw new ServerException("Transaction type GIFT_RECEIVE not found, please seed transaction types first");

        // 7. Create reference codes
        var sendRefCode = $"GIFT_SEND_{Guid.NewGuid():N}";
        var receiveRefCode = $"GIFT_RECEIVE_{Guid.NewGuid():N}";

        // 8. Create sender transaction (DEBIT) and ledger
        var sendTransaction = new Transaction
        {
            TransactionTypeId = giftSendTypeId.Value,
            UserId = sender.UserId,
            Amount = -totalCost,
            Currency = gift.Currency,
            TransactionContent = $"Tặng {cmd.Request.Quantity}x {gift.Name} cho {receiver.UserFullName}",
            Status = "completed",
            ReferenceCode = sendRefCode,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _transactionRepo.AddAsync(sendTransaction, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        senderWallet.Balance -= totalCost;
        senderWallet.UpdatedAt = DateTime.UtcNow;
        var senderLedger = new WalletLedger
        {
            WalletId = senderWallet.Id,
            TransactionId = sendTransaction.Id,
            Amount = -totalCost,
            BalanceAfter = senderWallet.Balance,
            EntryType = "DEBIT",
            Note = $"Tặng quà {gift.Name} (x{cmd.Request.Quantity})",
            CreatedAt = DateTime.UtcNow
        };
        senderWallet.WalletLedgers.Add(senderLedger);

        // 9. Create receiver transaction (CREDIT) and ledger
        var receiverWallet = await _walletRepo.GetByUserIdAsync(receiver.UserId, ct);
        if (receiverWallet == null)
        {
            receiverWallet = new ProjectLucy.Domain.Entities.Wallet
            {
                UserId = receiver.UserId,
                Balance = 0,
                Currency = "VND",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _walletRepo.AddAsync(receiverWallet, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        var receiveTransaction = new Transaction
        {
            TransactionTypeId = giftReceiveTypeId.Value,
            UserId = receiver.UserId,
            Amount = totalCost,
            Currency = gift.Currency,
            TransactionContent = $"Nhận {cmd.Request.Quantity}x {gift.Name} từ {sender.UserFullName}",
            Status = "completed",
            ReferenceCode = receiveRefCode,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _transactionRepo.AddAsync(receiveTransaction, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        receiverWallet.Balance += totalCost;
        receiverWallet.UpdatedAt = DateTime.UtcNow;
        var receiverLedger = new WalletLedger
        {
            WalletId = receiverWallet.Id,
            TransactionId = receiveTransaction.Id,
            Amount = totalCost,
            BalanceAfter = receiverWallet.Balance,
            EntryType = "CREDIT",
            Note = $"Nhận quà {gift.Name} (x{cmd.Request.Quantity})",
            CreatedAt = DateTime.UtcNow
        };
        receiverWallet.WalletLedgers.Add(receiverLedger);

        // 10. Create GiftTransaction
        var giftTxn = new GiftTransaction
        {
            TransactionId = sendTransaction.Id,
            SenderId = sender.UserId,
            ReceiverId = receiver.UserId,
            GiftId = gift.Id,
            RoomSessionId = cmd.Request.RoomSessionId,
            Quantity = cmd.Request.Quantity,
            TotalValue = totalCost,
            CreatedAt = DateTime.UtcNow
            ,IdempotencyKey = cmd.Request.IdempotencyKey
        };
        await _giftTxnRepo.AddAsync(giftTxn, ct);

        // 11. Save everything
        await _unitOfWork.SaveChangesAsync(ct);

        // 12. Return DTO
        var dto = new GiftTransactionDto
        {
            Id = giftTxn.Id,
            SenderId = sender.UserId,
            SenderName = sender.UserFullName,
            ReceiverId = receiver.UserId,
            ReceiverName = receiver.UserFullName,
            GiftId = gift.Id,
            GiftName = gift.Name,
            GiftIconUrl = gift.IconUrl,
            RoomSessionId = cmd.Request.RoomSessionId,
            Quantity = cmd.Request.Quantity,
            TotalValue = totalCost,
            CreatedAt = giftTxn.CreatedAt
        };

        return Result<GiftTransactionDto>.Success(dto, "Gift sent successfully");
    }

    private static GiftTransactionDto ToDto(GiftTransaction txn) => new()
    {
        Id = txn.Id,
        SenderId = txn.SenderId,
        SenderName = txn.Sender?.UserFullName ?? string.Empty,
        ReceiverId = txn.ReceiverId,
        ReceiverName = txn.Receiver?.UserFullName ?? string.Empty,
        GiftId = txn.GiftId,
        GiftName = txn.Gift?.Name ?? string.Empty,
        GiftIconUrl = txn.Gift?.IconUrl,
        RoomSessionId = txn.RoomSessionId,
        Quantity = txn.Quantity,
        TotalValue = txn.TotalValue,
        CreatedAt = txn.CreatedAt
    };
}
