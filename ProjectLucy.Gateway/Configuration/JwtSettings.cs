namespace ProjectLucy.Gateway.Configuration;

/// <summary>
/// Bản sao tối giản của JwtSettings (chỉ 3 field gateway cần) để validate Bearer JWT,
/// tránh phải reference ProjectLucy.Application (kéo theo EF/Npgsql/Infrastructure).
/// Bind từ section "JwtSettings" — cùng giá trị với ProjectLucy.API.
/// </summary>
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}
