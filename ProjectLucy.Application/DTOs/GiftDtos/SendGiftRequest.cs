namespace ProjectLucy.Application.DTOs.GiftDtos;

public class SendGiftRequest
{
    public Guid GiftId { get; set; }
    public Guid ReceiverId { get; set; }
    public int Quantity { get; set; } = 1;
    public Guid? RoomSessionId { get; set; }
    public Guid? IdempotencyKey { get; set; }
}

public class SendGiftToRoomRequest
{
    public Guid RoomSessionId { get; set; }
    public Guid GiftId { get; set; }
    public int Quantity { get; set; } = 1;
    public Guid IdempotencyKey { get; set; }
}
