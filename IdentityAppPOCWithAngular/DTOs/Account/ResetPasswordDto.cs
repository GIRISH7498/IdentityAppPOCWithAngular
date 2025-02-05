using System.ComponentModel.DataAnnotations;

namespace IdentityAppPOCWithAngular.DTOs.Account
{
    public class ResetPasswordDto
    {
        [Required]
        public string Token { get; set; }
        [Required]
        [RegularExpression("^[\\w\\.=-]+@[\\w\\.-]+\\.[\\w]{2,3}$", ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }
        [Required]
        [StringLength(15, MinimumLength = 6, ErrorMessage = "New Password must be at least {2}, and maximum {1} characters")]
        public string NewPassword { get; set; }
    }
}
