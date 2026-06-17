using ProjectLucy.Application.DTOs.LoginDtos.Childs;

namespace ProjectLucy.Application.DTOs.LoginDtos
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime AccessTokenExpiry { get; set; }
        public UserInfoDto User { get; set; } = null!;
    }
}
