using System.ComponentModel.DataAnnotations;

namespace TallerIsuzuWebApp.Models
{
    public class UserEditViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        // OPCIONAL y SIN mínimo
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required]
        public string Role { get; set; } = "User";

        public string? JobTitle { get; set; }
    }
}
