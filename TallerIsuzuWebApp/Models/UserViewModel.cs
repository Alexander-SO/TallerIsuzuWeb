namespace TallerIsuzuWebApp.Models
{
    public class UserViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; } // Solo para propósitos de edición
        public string Role { get; set; }
        public string JobTitle { get; set; }
        public string Nombre { get; set; } // Nuevo campo
    }


}
