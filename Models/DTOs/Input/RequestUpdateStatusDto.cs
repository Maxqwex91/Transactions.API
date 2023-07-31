using System.ComponentModel.DataAnnotations;

namespace Models.DTOs.Input
{
    public class RequestUpdateStatusDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int Status { get; set; }
    }
}
