using Microsoft.AspNetCore.Mvc;

namespace StrategieGameServer.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Lobby()
        {
            return View();
        }

    }
}