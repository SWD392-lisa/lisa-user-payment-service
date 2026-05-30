namespace ProjectLucy.Application.Auth.DTOs;

/// <summary>
/// Basic user info embedded in login / refresh-token responses.
/// Shape is identical to the old ProjectLucy.Shared version — no API contract change.
/// </summary>
public class UserInfoDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string RoleCode { get; set; } = null!;
    public string RoleName { get; set; } = null!;
}
