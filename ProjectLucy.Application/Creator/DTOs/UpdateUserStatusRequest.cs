using System.ComponentModel.DataAnnotations;

namespace ProjectLucy.Application.Creator.DTOs;

public sealed class UpdateUserStatusRequest
{
    public bool IsActive { get; init; }

    [MaxLength(500)]
    public string? Reason { get; init; }
}
