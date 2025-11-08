using System.ComponentModel.DataAnnotations;

namespace WorkingSpaces.Models.Dto
{
    public class UpdateUserDto
    {
        [StringLength(50)]
        public string? UserName { get; set; }

        [StringLength(500)]
        public string? FullName { get; set; }

        [Phone][StringLength(14)]
        public string? PhoneNumber { get; set; }

        [EmailAddress][StringLength(256)]
        public string? Email { get; set; }
    }
}