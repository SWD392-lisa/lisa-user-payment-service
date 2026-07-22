using System.ComponentModel.DataAnnotations;

namespace ProjectLucy.Application.DTOs.PrivacyDtos;

public class CreateRoomPersonaRequest
{
    [Required]
    public Guid RoomSessionId { get; set; }
}

public class RoomPersonaResponse
{
    public Guid RoomSessionId { get; set; }
    public Guid AnonymousId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string PersonaCode { get; set; } = null!;
    public string PersonaAssetUrl { get; set; } = null!;
    public string RoomAccessToken { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}
