using System.ComponentModel.DataAnnotations;

namespace Finsight.Commands
{
    public class LoginFSUserCommand
    {
        [Required]
        [EmailAddress]
        public  string Email { get; set; } = string.Empty;

        [Required]
        public  string Password { get; set; } = string.Empty;
    }
}