using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.DTOs.GiftDtos;

namespace ProjectLucy.Application.Gift.Queries.GetRoomGiftTransactions;

public class GetRoomGiftTransactionsQuery : IRequest<Result<IReadOnlyList<GiftTransactionDto>>>
{
    public Guid RoomSessionId { get; set; }
}
