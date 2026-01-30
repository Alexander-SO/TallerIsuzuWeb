using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using TallerIsuzuWebApp.Models;

namespace TallerIsuzuWebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ----------- LOGIN -----------
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            try
            {
                var cs = _configuration.GetConnectionString("Conn_STOD");
                using var cn = new SqlConnection(cs);
                using var cmd = new SqlCommand(
                    "SELECT Role, JobTitle, Nombre FROM SALL_Credenciales WHERE Username = @Username AND Password = @Password",
                    cn
                );
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Password", HashPassword(password));
                cn.Open();

                using var rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    string role = rd["Role"].ToString()!;
                    string jobTitle = rd["JobTitle"]?.ToString() ?? "N/A";
                    string nombre = rd["Nombre"].ToString()!;

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, username),
                        new Claim(ClaimTypes.Role, role),
                        new Claim("JobTitle", jobTitle),
                        new Claim("Nombre", nombre)
                    };

                    var identity = new ClaimsIdentity(claims, "CookieAuth");
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync("CookieAuth", principal);

                    return RedirectToAction("Index", "Home");
                }

                ViewBag.Error = "Usuario o contraseña incorrectos.";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error del sistema: {ex.Message}";
                return View();
            }
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }

        // ----------- LISTADO -----------
        [Authorize]
        public IActionResult Index()
        {
            var users = new List<UserListItemViewModel>();
            try
            {
                var cs = _configuration.GetConnectionString("Conn_STOD");
                using var cn = new SqlConnection(cs);
                using var cmd = new SqlCommand(
                    "SELECT Id, Username, Role, JobTitle, Nombre FROM SALL_Credenciales",
                    cn
                );
                cn.Open();
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    users.Add(new UserListItemViewModel
                    {
                        Id = (int)rd["Id"],
                        Username = rd["Username"].ToString()!,
                        Role = rd["Role"].ToString()!,
                        JobTitle = rd["JobTitle"]?.ToString(),
                        Nombre = rd["Nombre"].ToString()!
                    });
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al obtener la lista de usuarios: {ex.Message}";
            }

            return View(users);
        }

        // ----------- CREAR -----------
        [AllowAnonymous]
        [HttpGet]
        public IActionResult CreateUser() => View(new UserCreateViewModel());
        
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(UserCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var cs = _configuration.GetConnectionString("Conn_STOD");
                using var cn = new SqlConnection(cs);
                using var cmd = new SqlCommand(
                    @"INSERT INTO SALL_Credenciales (Username, Password, Role, JobTitle, Nombre)
                      VALUES (@Username, @Password, @Role, @JobTitle, @Nombre)",
                    cn
                );
                cmd.Parameters.AddWithValue("@Username", model.Username.Trim());
                cmd.Parameters.AddWithValue("@Password", HashPassword(model.Password));
                cmd.Parameters.AddWithValue("@Role", model.Role.Trim());
                cmd.Parameters.AddWithValue("@JobTitle", string.IsNullOrWhiteSpace(model.JobTitle) ? (object)DBNull.Value : model.JobTitle.Trim());
                cmd.Parameters.AddWithValue("@Nombre", model.Nombre.Trim());

                cn.Open();
                cmd.ExecuteNonQuery();

                TempData["Message"] = "Usuario creado exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al crear el usuario: {ex.Message}";
                return View(model);
            }
        }

        // ----------- EDITAR -----------
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            try
            {
                var cs = _configuration.GetConnectionString("Conn_STOD");
                using var cn = new SqlConnection(cs);
                using var cmd = new SqlCommand(
                    "SELECT Id, Username, Role, JobTitle, Nombre FROM SALL_Credenciales WHERE Id = @Id",
                    cn
                );
                cmd.Parameters.AddWithValue("@Id", id);
                cn.Open();

                using var rd = cmd.ExecuteReader();
                if (!rd.Read())
                {
                    TempData["Error"] = "Usuario no encontrado.";
                    return RedirectToAction("Index");
                }

                var vm = new UserEditViewModel
                {
                    Id = (int)rd["Id"],
                    Username = rd["Username"].ToString()!,
                    Role = rd["Role"].ToString()!,
                    JobTitle = rd["JobTitle"]?.ToString(),
                    Nombre = rd["Nombre"].ToString()!
                };
                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al obtener los datos del usuario: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(UserEditViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var cs = _configuration.GetConnectionString("Conn_STOD");
                using var cn = new SqlConnection(cs);
                cn.Open();
                using var tx = cn.BeginTransaction();

                // Actualiza datos básicos
                using (var cmd = new SqlCommand(
                    @"UPDATE SALL_Credenciales
                      SET Username = @Username,
                          Role     = @Role,
                          JobTitle = @JobTitle,
                          Nombre   = @Nombre
                      WHERE Id = @Id;", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", model.Id);
                    cmd.Parameters.AddWithValue("@Username", model.Username.Trim());
                    cmd.Parameters.AddWithValue("@Role", model.Role.Trim());
                    cmd.Parameters.AddWithValue("@JobTitle", string.IsNullOrWhiteSpace(model.JobTitle) ? (object)DBNull.Value : model.JobTitle.Trim());
                    cmd.Parameters.AddWithValue("@Nombre", model.Nombre.Trim());
                    cmd.ExecuteNonQuery();
                }

                // Si viene contraseña (no obligatoria), la actualiza
                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    using var cmdPwd = new SqlCommand(
                        @"UPDATE SALL_Credenciales SET Password = @Password WHERE Id = @Id;",
                        cn, tx
                    );
                    cmdPwd.Parameters.AddWithValue("@Id", model.Id);
                    cmdPwd.Parameters.AddWithValue("@Password", HashPassword(model.Password));
                    cmdPwd.ExecuteNonQuery();
                }

                tx.Commit();
                TempData["Message"] = "Usuario actualizado correctamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al actualizar el usuario: {ex.Message}";
                return View(model);
            }
        }

        // ----------- UTIL -----------
        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashBytes);
        }
    }
}
