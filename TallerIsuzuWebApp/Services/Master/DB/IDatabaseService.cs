using System.Threading.Tasks;

public interface IDatabaseService
{
    Task<SPResponse<object>> EjecutarSP_Dinamico(SPDynamicRequest request);
}
