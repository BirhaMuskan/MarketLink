using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Farmer")]
    public class FarmerController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View("Dashboard");
        }
    }
}
