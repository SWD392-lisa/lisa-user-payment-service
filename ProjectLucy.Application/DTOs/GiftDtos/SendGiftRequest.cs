namespace ProjectLucy.Application.DTOs.GiftDtos;

public class SendGiftRequest
{
    public Guid GiftId { get; set; }
    public Guid ReceiverId { get; set; }
    public int Quantity { get; set; } = 1;
    public Guid? RoomSessionId { get; set; }
}
