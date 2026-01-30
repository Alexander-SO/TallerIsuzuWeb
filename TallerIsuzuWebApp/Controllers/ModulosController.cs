using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TallerIsuzuWebApp.Controllers
{
    [Authorize]
    public class ModulosController : Controller
    {
        public IActionResult Ejemplo()
        {
            return View();
        }
    }
}
