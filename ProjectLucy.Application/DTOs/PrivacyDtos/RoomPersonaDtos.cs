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

public class RoomParticipantIdentitiesRequest
{
    [Required]
    public Guid RoomSessionId { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public List<Guid> AnonymousIds { get; set; } = [];
}

public class RoomParticipantIdentityResponse
{
    public Guid AnonymousId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
}
