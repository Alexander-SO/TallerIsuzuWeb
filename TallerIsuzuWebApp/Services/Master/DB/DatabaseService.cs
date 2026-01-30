using Microsoft.Data.SqlClient;
using System.Data;

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Conn_STOD");
    }

    public async Task<SPResponse<object>> EjecutarSP_Dinamico(SPDynamicRequest request)
    {
        var response = new SPResponse<object>();

        try
        {
            if (string.IsNullOrEmpty(request.NombreSP))
            {
                response.Codigo = -1;
                response.Mensaje = "El nombre del procedimiento almacenado es obligatorio.";
                return response;
            }

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(request.NombreSP, conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            if (request.Parametros != null)
            {
                foreach (var param in request.Parametros)
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }

            await conn.OpenAsync();

            switch (request.TipoRetorno?.ToLower())
            {
                case "tabla":
                    var dt = new DataTable();
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                    response.Codigo = 0;
                    response.Mensaje = "Ejecución exitosa.";
                    response.Data = dt;
                    break;

                case "int":
                case "varchar":
                case "bool":
                case "decimal":
                case "float":
                    var scalar = await cmd.ExecuteScalarAsync();
                    response.Codigo = 0;
                    response.Mensaje = "Ejecución exitosa.";
                    response.Data = ConvertScalar(scalar, request.TipoRetorno);
                    break;

                case "none":
                    await cmd.ExecuteNonQueryAsync();
                    response.Codigo = 0;
                    response.Mensaje = "Ejecución exitosa (sin retorno).";
                    response.Data = null;
                    break;

                default:
                    response.Codigo = -1;
                    response.Mensaje = "TipoRetorno inválido. Use: tabla, int, varchar, bool, decimal, float, none.";
                    break;
            }
        }
        catch (Exception ex)
        {
            response.Codigo = -1;
            response.Mensaje = $"Error: {ex.Message}";
            response.Data = null;
        }

        return response;
    }

    private object ConvertScalar(object scalar, string tipo)
    {
        if (scalar == DBNull.Value || scalar == null) return null;

        return tipo switch
        {
            "int" => Convert.ToInt32(scalar),
            "varchar" => scalar.ToString(),
            "bool" => Convert.ToBoolean(scalar),
            "decimal" => Convert.ToDecimal(scalar),
            "float" => Convert.ToSingle(scalar),
            _ => scalar.ToString()
        };
    }
}
