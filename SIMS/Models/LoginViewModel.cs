using System.ComponentModel.DataAnnotations;

namespace SIMS.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username can be not empty")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Password can be not empty")]
        public string Password { get; set; } = null!;
    }
}
