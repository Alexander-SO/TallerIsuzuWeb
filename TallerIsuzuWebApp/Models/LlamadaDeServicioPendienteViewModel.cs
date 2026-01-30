// Models/LlamadaDeServicioPendienteViewModel.cs
public class LlamadaDeServicioPendienteViewModel
{
    public string CallID { get; set; } = "";
    public string InternalSN { get; set; } = "";
    public string UPlacas { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string Name { get; set; } = "";

    // NUEVOS:
    public string? Servicios { get; set; }   // "Lavado exterior, Aspirado"
    public int? Prioridad { get; set; }      // 1..10
}
