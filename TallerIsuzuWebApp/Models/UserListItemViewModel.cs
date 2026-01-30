namespace TallerIsuzuWebApp.Models
{
    public class UserListItemViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }
}
