using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.DTOs.GiftDtos;

namespace ProjectLucy.Application.Gift.Commands.SendGift;

public class SendGiftCommand : IRequest<Result<GiftTransactionDto>>
{
    public SendGiftRequest Request { get; set; } = null!;
    public Guid SenderId { get; set; }
}
