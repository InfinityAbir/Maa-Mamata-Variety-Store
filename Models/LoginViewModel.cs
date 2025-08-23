using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Models
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public Role Role { get; set; }  // Must match enum
    }
}
