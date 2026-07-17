using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.DTOs.GiftDtos;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Application.Gift.Queries.GetRoomGiftTransactions;

public class GetRoomGiftTransactionsQueryHandler : IRequestHandler<GetRoomGiftTransactionsQuery, Result<IReadOnlyList<GiftTransactionDto>>>
{
    private readonly IGiftTransactionRepository _giftTxnRepo;

    public GetRoomGiftTransactionsQueryHandler(IGiftTransactionRepository giftTxnRepo)
    {
        _giftTxnRepo = giftTxnRepo;
    }

    public async Task<Result<IReadOnlyList<GiftTransactionDto>>> Handle(GetRoomGiftTransactionsQuery request, CancellationToken ct)
    {
        var txns = await _giftTxnRepo.GetBySessionAsync(request.RoomSessionId, ct);

        var dtos = txns.Select(t => new GiftTransactionDto
        {
            Id = t.Id,
            SenderId = t.SenderId,
            SenderName = t.Sender.UserFullName,
            ReceiverId = t.ReceiverId,
            ReceiverName = t.Receiver.UserFullName,
            GiftId = t.GiftId,
            GiftName = t.Gift.Name,
            GiftIconUrl = t.Gift.IconUrl,
            RoomSessionId = t.RoomSessionId,
            Quantity = t.Quantity,
            TotalValue = t.TotalValue,
            CreatedAt = t.CreatedAt
        }).ToList();

        return Result<IReadOnlyList<GiftTransactionDto>>.Success(dtos, "Gift transactions retrieved");
    }
}
