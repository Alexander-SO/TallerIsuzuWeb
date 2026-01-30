using System.ComponentModel.DataAnnotations;

namespace TallerIsuzuWebApp.Models
{
    public class UserCreateViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        // SIN mínimo; permite códigos cortos (p. ej. 4 dígitos)
        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "User";

        public string? JobTitle { get; set; }
    }
}
