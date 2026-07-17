namespace ProjectLucy.Application.DTOs.GiftDtos;

public class GiftTransactionDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public Guid ReceiverId { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public Guid GiftId { get; set; }
    public string GiftName { get; set; } = string.Empty;
    public string? GiftIconUrl { get; set; }
    public Guid? RoomSessionId { get; set; }
    public int Quantity { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime CreatedAt { get; set; }
}
