using Microsoft.AspNetCore.Mvc;

namespace MarketLink.Controllers
{
    public class FarmersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int id)
        {
            ViewBag.FarmerId = id;
            return View();
        }
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}