public class SPDynamicRequest
{
    public string NombreSP { get; set; }
    public Dictionary<string, object>? Parametros { get; set; }
    public string TipoRetorno { get; set; } // tabla, int, varchar, bool, decimal, float, none
}
