using Microsoft.AspNetCore.Mvc;

namespace ClixyTube.Controllers
{
    public class SwaggerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
