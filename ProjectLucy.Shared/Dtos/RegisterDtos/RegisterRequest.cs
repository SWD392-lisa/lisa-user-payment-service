using System.ComponentModel.DataAnnotations;

namespace ProjectLucy.Shared.Dtos.RegisterDtos
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(255, ErrorMessage = "Full name cannot exceed 255 characters")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(255)]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = null!;

        [Required(ErrorMessage = "Birthday is required")]
        public DateOnly Birthday { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format")]
        [MaxLength(30)]
        public string? PhoneNumber { get; set; }
    }
}
