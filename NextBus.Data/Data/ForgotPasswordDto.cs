using System.ComponentModel.DataAnnotations;

namespace NextBus.Data.Data
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Email is not valid")]
        public string Email { get; set; }
    }
}
