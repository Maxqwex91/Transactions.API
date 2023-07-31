using System.ComponentModel.DataAnnotations;

namespace Models.DTOs.Input
{
    public class SignUpDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
