using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using TallerIsuzuWebApp.Models;

namespace TallerIsuzuWebApp.Controllers
{
    // Control Llamadas de Servicio Lavado (CLSL)
    public class CLSLController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IAntiforgery _antiforgery;

        public CLSLController(IAntiforgery antiforgery, IConfiguration configuration)
        {
            _antiforgery = antiforgery;
            _configuration = configuration;
        }

        [Authorize]
        public IActionResult ListadoCompleto()
        {
            var llamadas = new List<LlamadaDeServicioViewModel>();
            var connectionString = _configuration.GetConnectionString("Conn_STOD");

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand("SALL_GET_llamadasDeServicio_listadoCompleto", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        llamadas.Add(new LlamadaDeServicioViewModel
                        {
                            CallID = reader["callID"].ToString(),
                            InternalSN = reader["internalSN"].ToString(),
                            UPlacas = reader["U_Placas"].ToString(),
                            ItemName = reader["itemName"].ToString(),
                            Name = reader["Name"].ToString(),
                            NombreLavador = reader["nombreLavador"].ToString(),
                            Estado = reader["estado"].ToString()
                        });
                    }
                }
            }

            return View(llamadas);
        }

        [Authorize]
        public IActionResult PendientesDeAsignar()
        {
            var llamadas = new List<LlamadaDeServicioPendienteViewModel>();
            var connectionString = _configuration.GetConnectionString("Conn_STOD");

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand("SALL_GET_llamadasDeServicio_pendientesDeAsignar", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var vm = new LlamadaDeServicioPendienteViewModel
                        {
                            CallID = reader["callID"].ToString(),
                            InternalSN = reader["internalSN"].ToString(),
                            UPlacas = reader["U_Placas"].ToString(),
                            ItemName = reader["itemName"].ToString(),
                            Name = reader["Name"].ToString()
                        };

                        // Nuevas columnas (si existen en el SP)
                        if (reader.HasColumn("Servicios") && !(reader["Servicios"] is DBNull))
                            vm.Servicios = reader["Servicios"].ToString();

                        if (reader.HasColumn("Prioridad") && !(reader["Prioridad"] is DBNull))
                            vm.Prioridad = Convert.ToInt32(reader["Prioridad"]);

                        llamadas.Add(vm);
                    }
                }
            }

            return View(llamadas);
        }

        [HttpGet]
        public JsonResult GetTecnicos()
        {
            var tecnicos = new List<TecnicoViewModel>();
            var connectionString = _configuration.GetConnectionString("Conn_STOD");

            using (var connection = new SqlConnection(connectionString))
            {
                var query = "SELECT Username, Nombre FROM SALL_Credenciales WHERE LOWER(Role) IN ('tecnico','supervisor') AND Username <> 'none1'";
                var command = new SqlCommand(query, connection);

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tecnicos.Add(new TecnicoViewModel
                        {
                            Username = reader["Username"].ToString(),
                            Nombre = reader["Nombre"].ToString()
                        });
                    }
                }
            }

            return Json(tecnicos);
        }

        [HttpPost]
        public JsonResult AsignarLlamada([FromBody] AsignarLlamadaViewModel model)
        {
            var response = new { success = false, message = "Error al asignar la llamada." };

            if (string.IsNullOrEmpty(model.CallID) || string.IsNullOrEmpty(model.UsuarioLavador))
            {
                response = new { success = false, message = "Parámetros inválidos. Asegúrese de seleccionar una llamada y un técnico." };
                return Json(response);
            }

            var connectionString = _configuration.GetConnectionString("Conn_STOD");

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var command = new SqlCommand("SALL_UPDATE_asignarLlamada", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    command.Parameters.AddWithValue("@callID", model.CallID);
                    command.Parameters.AddWithValue("@usuarioLavador", model.UsuarioLavador);

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                response = new { success = true, message = "Llamada asignada exitosamente." };
            }
            catch (Exception ex)
            {
                response = new { success = false, message = $"Error al asignar la llamada: {ex.Message}" };
            }

            return Json(response);
        }

        [Authorize]
        public IActionResult VerLlamadasAsignadas()
        {
            var llamadasAsignadas = new List<LlamadaDeServicioAsignadaViewModel>();
            var connectionString = _configuration.GetConnectionString("Conn_STOD");

            // Usuario actual
            var user = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(user))
            {
                return Unauthorized();
            }

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand("SALL_GET_llamadasDeServicio_Asignadas", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@user", user);

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        llamadasAsignadas.Add(new LlamadaDeServicioAsignadaViewModel
                        {
                            CallID = reader["callID"].ToString(),
                            InternalSN = reader["internalSN"].ToString(),
                            UPlacas = reader["U_Placas"].ToString(),
                            ItemName = reader["itemName"].ToString(),
                            Name = reader["Name"].ToString(),
                            UsuarioLavador = reader["usuarioLavador"].ToString(),
                            FechaAsignacion = reader["fechaAsignacion"].ToString()
                        });
                    }
                }
            }

            // Token antifalsificación (si lo usas en la vista)
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            ViewData["AntiforgeryToken"] = tokens.RequestToken;

            return View(llamadasAsignadas);
        }

        [HttpPost]
        [Authorize]
        public JsonResult ActualizarEstadoLlamada([FromBody] LlamadaEstadoRequest request)
        {
            var connectionString = _configuration.GetConnectionString("Conn_STOD");
            var user = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(user))
            {
                return Json(new { success = false, message = "Usuario no autorizado." });
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var command = new SqlCommand("SALL_UPDATE_asignarEstadoLlamada", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    command.Parameters.AddWithValue("@callID", request.CallID);
                    command.Parameters.AddWithValue("@usuarioLavador", user);
                    command.Parameters.AddWithValue("@estado", request.Estado);

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                return Json(new { success = true, message = "Estado actualizado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al actualizar el estado: {ex.Message}" });
            }
        }

        // Clase para manejar la solicitud
        public class LlamadaEstadoRequest
        {
            public int CallID { get; set; }
            public string Estado { get; set; }
        }

        [Authorize]
        public IActionResult ObtenerLlamadasComisiones()
        {
            var llamadas = new List<LlamadaDeServicioFinalizadaViewModel>();
            var connectionString = _configuration.GetConnectionString("Conn_STOD");

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand("SALL_GET_llamadasDeServicio_LlamadasFinalizadas", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        llamadas.Add(new LlamadaDeServicioFinalizadaViewModel
                        {
                            CallID = reader["callID"].ToString(),
                            FechaCreacion = reader.GetDateTime(reader.GetOrdinal("fechaCreacion")),
                            FechaCierre = reader.GetDateTime(reader.GetOrdinal("fechaCierre")),
                            InternalSN = reader["internalSN"].ToString(),
                            UPlacas = reader["U_Placas"].ToString(),
                            ItemName = reader["itemName"].ToString(),
                            Name = reader["Name"].ToString(),
                            UsuarioLavador = reader["usuarioLavador"].ToString(),
                            Estado = reader["estado"].ToString()
                        });
                    }
                }
            }

            return View(llamadas);
        }

        [HttpPost]
        [Authorize]
        public JsonResult ActualizarEstadoPreComision([FromBody] List<int> callIDs)
        {
            if (callIDs == null || !callIDs.Any())
            {
                return Json(new { success = false, message = "No se seleccionaron llamadas." });
            }

            var connectionString = _configuration.GetConnectionString("Conn_STOD");

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    foreach (var callID in callIDs)
                    {
                        var command = new SqlCommand("SALL_UPDATE_LlamadasEstadoPreComision", connection)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        command.Parameters.AddWithValue("@CallID", callID);
                        command.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Estado actualizado a 'preComision' correctamente para las llamadas seleccionadas." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al actualizar el estado: {ex.Message}" });
            }
        }

        [HttpPost]
        public JsonResult GuardarLlamadasComisiones([FromBody] List<LlamadaDeServicioFinalizadaViewModel> selectedCalls)
        {
            if (selectedCalls == null || !selectedCalls.Any())
            {
                return Json(new { success = false, message = "No se recibieron llamadas para guardar." });
            }

            var connectionString = _configuration.GetConnectionString("Conn_STOD");
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    foreach (var llamada in selectedCalls)
                    {
                        var command = new SqlCommand("SALL_INSERT_GuardarLlamadaParaComision", connection)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        command.Parameters.AddWithValue("@CallID", llamada.CallID);
                        command.Parameters.AddWithValue("@FechaCreacion", llamada.FechaCreacion);
                        command.Parameters.AddWithValue("@FechaCierre", llamada.FechaCierre);
                        command.Parameters.AddWithValue("@InternalSN", llamada.InternalSN);
                        command.Parameters.AddWithValue("@UPlacas", llamada.UPlacas);
                        command.Parameters.AddWithValue("@ItemName", llamada.ItemName);
                        command.Parameters.AddWithValue("@Name", llamada.Name);
                        command.Parameters.AddWithValue("@UsuarioLavador", string.IsNullOrEmpty(llamada.UsuarioLavador) ? (object)DBNull.Value : llamada.UsuarioLavador);
                        command.Parameters.AddWithValue("@Estado", llamada.Estado);

                        command.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Llamadas guardadas correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al guardar las llamadas: {ex.Message}" });
            }
        }

        // Gestión de Códigos de Mano de Obra - Listado
        [Authorize]
        public IActionResult GestionManoDeObra()
        {
            var codigos = new List<CodigoDeManoDeObraViewModel>();
            var connectionString = _configuration.GetConnectionString("Conn_STOD");

            using (var connection = new SqlConnection(connectionString))
            {
                var query = "SELECT Id, CodigoManoDeObra, ValorComision, CondicionesAdicionales FROM SALL_CodigosDeManoDeObraLavado";
                var command = new SqlCommand(query, connection);

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        codigos.Add(new CodigoDeManoDeObraViewModel
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            CodigoManoDeObra = reader["CodigoManoDeObra"].ToString(),
                            ValorComision = reader.GetDecimal(reader.GetOrdinal("ValorComision")),
                            CondicionesAdicionales = reader["CondicionesAdicionales"].ToString()
                        });
                    }
                }
            }

            return View(codigos);
        }

        // Crear nuevo código de mano de obra
        [HttpGet]
        [Authorize]
        public IActionResult CreateCodigoManoDeObra()
        {
            var model = new CodigoDeManoDeObraViewModel();
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public IActionResult CreateCodigoManoDeObra(CodigoDeManoDeObraViewModel model)
        {
            var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "Guest";

            if (userRole != "Desarrollador")
            {
                model.CondicionesAdicionales = null; // Ignorar el campo para roles distintos
            }

            if (ModelState.IsValid)
            {
                var connectionString = _configuration.GetConnectionString("Conn_STOD");

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Verificar duplicado
                    var checkQuery = "SELECT COUNT(*) FROM SALL_CodigosDeManoDeObraLavado WHERE CodigoManoDeObra = @CodigoManoDeObra";
                    using (var checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@CodigoManoDeObra", model.CodigoManoDeObra);
                        var exists = (int)checkCommand.ExecuteScalar() > 0;

                        if (exists)
                        {
                            ModelState.AddModelError("CodigoManoDeObra", "Código ya ingresado, revisar el listado de códigos.");
                            return View(model);
                        }
                    }

                    var query = @"INSERT INTO SALL_CodigosDeManoDeObraLavado
                                  (CodigoManoDeObra, ValorComision, CondicionesAdicionales)
                                  VALUES (@CodigoManoDeObra, @ValorComision, @CondicionesAdicionales)";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CodigoManoDeObra", model.CodigoManoDeObra);
                        command.Parameters.AddWithValue("@ValorComision", model.ValorComision);
                        command.Parameters.AddWithValue("@CondicionesAdicionales", model.CondicionesAdicionales ?? (object)DBNull.Value);
                        command.ExecuteNonQuery();
                    }
                }

                return RedirectToAction("GestionManoDeObra");
            }

            return View(model);
        }

        // Editar código de mano de obra
        [HttpGet]
        [Authorize]
        public IActionResult EditCodigoManoDeObra(int id)
        {
            var connectionString = _configuration.GetConnectionString("Conn_STOD");
            var codigo = new CodigoDeManoDeObraViewModel();

            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"SELECT Id, CodigoManoDeObra, ValorComision, CondicionesAdicionales
                              FROM SALL_CodigosDeManoDeObraLavado WHERE Id = @Id";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        codigo.Id = reader.GetInt32(reader.GetOrdinal("Id"));
                        codigo.CodigoManoDeObra = reader["CodigoManoDeObra"].ToString();
                        codigo.ValorComision = reader.GetDecimal(reader.GetOrdinal("ValorComision"));
                        codigo.CondicionesAdicionales = reader["CondicionesAdicionales"].ToString();
                    }
                }
            }

            return View(codigo);
        }

        [HttpPost]
        [Authorize]
        public IActionResult EditCodigoManoDeObra(CodigoDeManoDeObraViewModel model)
        {
            var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "Guest";

            if (userRole != "Desarrollador")
            {
                model.CondicionesAdicionales = null; // Ignorar el campo para roles distintos
            }

            if (ModelState.IsValid)
            {
                var connectionString = _configuration.GetConnectionString("Conn_STOD");

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Verificar duplicado en otro registro
                    var checkQuery = @"SELECT COUNT(*) FROM SALL_CodigosDeManoDeObraLavado
                                       WHERE CodigoManoDeObra = @CodigoManoDeObra AND Id <> @Id";
                    using (var checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@CodigoManoDeObra", model.CodigoManoDeObra);
                        checkCommand.Parameters.AddWithValue("@Id", model.Id);
                        var exists = (int)checkCommand.ExecuteScalar() > 0;

                        if (exists)
                        {
                            ModelState.AddModelError("CodigoManoDeObra", "Código ya ingresado, revisar el listado de códigos.");
                            return View(model);
                        }
                    }

                    var updateQuery = @"UPDATE SALL_CodigosDeManoDeObraLavado
                                        SET CodigoManoDeObra = @CodigoManoDeObra,
                                            ValorComision = @ValorComision,
                                            CondicionesAdicionales = @CondicionesAdicionales
                                        WHERE Id = @Id";
                    using (var updateCommand = new SqlCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@Id", model.Id);
                        updateCommand.Parameters.AddWithValue("@CodigoManoDeObra", model.CodigoManoDeObra);
                        updateCommand.Parameters.AddWithValue("@ValorComision", model.ValorComision);
                        updateCommand.Parameters.AddWithValue("@CondicionesAdicionales", model.CondicionesAdicionales ?? (object)DBNull.Value);

                        updateCommand.ExecuteNonQuery();
                    }
                }

                return RedirectToAction("GestionManoDeObra");
            }

            return View(model);
        }

        // Eliminar código de mano de obra
        [HttpPost]
        [Authorize]
        public IActionResult DeleteCodigoManoDeObra(int id)
        {
            var connectionString = _configuration.GetConnectionString("Conn_STOD");

            using (var connection = new SqlConnection(connectionString))
            {
                var query = "DELETE FROM SALL_CodigosDeManoDeObraLavado WHERE Id = @Id";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                connection.Open();
                command.ExecuteNonQuery();
            }

            return RedirectToAction("GestionManoDeObra");
        }

        [Authorize]
        public IActionResult AjusteComisiones()
        {
            var comisiones = new List<SALLComisionesLavadoViewModel>();
            var connectionString = _configuration.GetConnectionString("Conn_STOD");

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand("SELECT * FROM SALL_ComisionesLavado", connection);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        comisiones.Add(new SALLComisionesLavadoViewModel
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("id")),
                            DocNumber = reader.IsDBNull(reader.GetOrdinal("DocNumber")) ? null : reader.GetInt32(reader.GetOrdinal("DocNumber")),
                            DocStatus = reader["DocStatus"].ToString(),
                            SrcvCallID = reader["SrcvCallID"].ToString(),
                            ItemCode = reader["ItemCode"].ToString(),
                            ValorComision = reader.GetDecimal(reader.GetOrdinal("ValorComision")),
                            LavadoTecnico = reader["LavadoTecnico"].ToString()
                        });
                    }
                }
            }

            return View(comisiones);
        }

        [HttpPost]
        [Authorize]
        public JsonResult ActualizarComision([FromBody] ActualizarComisionRequest request)
        {
            var response = new { success = false, message = "Error al actualizar la comisión." };

            if (request.Id <= 0 || request.ValorComision <= 0)
            {
                response = new { success = false, message = "Datos inválidos." };
                return Json(response);
            }

            var connectionString = _configuration.GetConnectionString("Conn_STOD");

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var command = new SqlCommand("UPDATE SALL_ComisionesLavado SET ValorComision = @ValorComision WHERE id = @Id", connection);
                    command.Parameters.AddWithValue("@Id", request.Id);
                    command.Parameters.AddWithValue("@ValorComision", request.ValorComision);

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                response = new { success = true, message = "Comisión actualizada exitosamente." };
            }
            catch (Exception ex)
            {
                response = new { success = false, message = $"Error: {ex.Message}" };
            }

            return Json(response);
        }

        [HttpPost]
        [Authorize]
        public JsonResult MarcarEliminado([FromBody] MarcarEliminadoRequest request)
        {
            var response = new { success = false, message = "Error al marcar como eliminado." };

            if (request.Id <= 0)
            {
                response = new { success = false, message = "ID inválido." };
                return Json(response);
            }

            var connectionString = _configuration.GetConnectionString("Conn_STOD");

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var command = new SqlCommand("UPDATE SALL_ComisionesLavado SET DocStatus = 'EL' WHERE id = @Id", connection);
                    command.Parameters.AddWithValue("@Id", request.Id);

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                response = new { success = true, message = "Registro marcado como eliminado." };
            }
            catch (Exception ex)
            {
                response = new { success = false, message = $"Error: {ex.Message}" };
            }

            return Json(response);
        }

        [HttpPost]
        public JsonResult AsignarLlamadaCompartida([FromBody] AsignarLlamadaViewModel model)
        {
            if (string.IsNullOrEmpty(model.CallID) || string.IsNullOrEmpty(model.UsuarioLavador))
            {
                return Json(new { success = false, message = "Parámetros inválidos." });
            }

            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("Conn_STOD")))
                {
                    var command = new SqlCommand("SALL_UPDATE_asignarLlamadaCompartida", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    command.Parameters.AddWithValue("@callID", model.CallID);
                    command.Parameters.AddWithValue("@usuarioLavador", model.UsuarioLavador); // tec1;tec2;tec3

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                return Json(new { success = true, message = "Asignación compartida guardada exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al guardar asignación: " + ex.Message });
            }
        }
        // Listado priorizado (opcional; puedes reutilizar PendientesDeAsignar si prefieres)
        [Authorize]
        public IActionResult PendientesDeAsignarPrioritarias()
        {
            var llamadas = new List<LlamadaDeServicioPendienteViewModel>();
            var cs = _configuration.GetConnectionString("Conn_STOD");

            using var cn = new SqlConnection(cs);
            using var cmd = new SqlCommand("SALL_GET_llamadasDeServicio_pendientesDeAsignar", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                var vm = new LlamadaDeServicioPendienteViewModel
                {
                    CallID = rd["callID"].ToString(),
                    InternalSN = rd["internalSN"].ToString(),
                    UPlacas = rd["U_Placas"].ToString(),
                    ItemName = rd["itemName"].ToString(),
                    Name = rd["Name"].ToString()
                };
                if (rd.HasColumn("Servicios") && !(rd["Servicios"] is DBNull)) vm.Servicios = rd["Servicios"].ToString();
                if (rd.HasColumn("Prioridad") && !(rd["Prioridad"] is DBNull)) vm.Prioridad = Convert.ToInt32(rd["Prioridad"]);
                llamadas.Add(vm);
            }

            // La vista nueva ordena por prioridad, pero también puedes ordenar aquí:
            // llamadas = llamadas.OrderByDescending(x => x.Prioridad ?? 0).ToList();

            return View("PendientesDeAsignarPrioritarias", llamadas);
        }

        // Guardar prioridad en tabla nueva
        [HttpPost]
        public JsonResult GuardarPrioridad([FromBody] GuardarPrioridadRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.CallID) || req.Prioridad < 1 || req.Prioridad > 10)
                return Json(new { success = false, message = "Datos inválidos." });

            try
            {
                using var cn = new SqlConnection(_configuration.GetConnectionString("Conn_STOD"));
                cn.Open();

                // Opción A: usar SP (recomendado)
                using var cmd = new SqlCommand("SALL_INSERT_LlamadaPrioritaria", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@CallID", req.CallID);
                cmd.Parameters.AddWithValue("@Prioridad", req.Prioridad);
                cmd.Parameters.AddWithValue("@CreadoPor", User?.Identity?.Name ?? "sistema");
                cmd.ExecuteNonQuery();

                // Opción B inline (si aún no tienes el SP):
                // using var cmd = new SqlCommand(@"INSERT INTO SALL_LavadoPrioridades(CallID, Prioridad, CreadoPor)
                //                                  VALUES (@CallID, @Prioridad, @CreadoPor)", cn);

                return Json(new { success = true, message = "Prioridad guardada correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al guardar prioridad: " + ex.Message });
            }
        }

        public class GuardarPrioridadRequest
        {
            public string CallID { get; set; } = "";
            public int Prioridad { get; set; }
        }

    }

    // <= FIN controller
}

// Métodos de extensión: deben estar en clase estática **no anidada**
namespace TallerIsuzuWebApp.Controllers
{
    internal static class DataReaderExtensions
    {
        public static bool HasColumn(this IDataRecord dr, string columnName)
        {
            for (int i = 0; i < dr.FieldCount; i++)
                if (dr.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
