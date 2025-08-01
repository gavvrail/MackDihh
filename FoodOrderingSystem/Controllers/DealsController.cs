using Microsoft.AspNetCore.Mvc;

namespace FoodOrderingSystem.Controllers
{
    public class DealsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
