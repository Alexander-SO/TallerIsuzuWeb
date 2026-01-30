namespace TallerIsuzuWebApp.Models
{
    public class SALLComisionesLavado
    {
        public int Id { get; set; }
        public int? DocNumber { get; set; }
        public string DocStatus { get; set; }
        public string SrcvCallID { get; set; }
        public string ItemCode { get; set; }
        public decimal ValorComision { get; set; }
        public string LavadoTecnico { get; set; }
    }

    public class ComisionUpdateRequest
    {
        public int Id { get; set; }
        public decimal ValorComision { get; set; }
    }

    public class MarcarEliminadoRequest
    {
        public int Id { get; set; }
    }

    public class ActualizarComisionRequest
    {
        public int Id { get; set; }
        public decimal ValorComision { get; set; }
    }


    public class SALLComisionesLavadoViewModel
    {
        public int Id { get; set; }
        public int? DocNumber { get; set; }
        public string DocStatus { get; set; }
        public string SrcvCallID { get; set; }
        public string ItemCode { get; set; }
        public decimal ValorComision { get; set; }
        public string LavadoTecnico { get; set; }
    }
}
