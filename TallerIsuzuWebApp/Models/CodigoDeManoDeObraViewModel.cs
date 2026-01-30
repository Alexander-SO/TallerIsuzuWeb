namespace TallerIsuzuWebApp.Models
{
    public class CodigoDeManoDeObraViewModel
    {
        public int Id { get; set; }

        public string CodigoManoDeObra { get; set; }

        public decimal ValorComision { get; set; }

        // Propiedad opcional
        public string? CondicionesAdicionales { get; set; }
    }


}
