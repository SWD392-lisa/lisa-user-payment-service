using System.ComponentModel.DataAnnotations;

namespace ProjectLucy.Shared.Dtos.RefreshTokenDtos
{
    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; } = null!;
    }
}
