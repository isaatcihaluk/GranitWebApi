using Microsoft.AspNetCore.Mvc;

namespace GranitWebApi.Controllers
{
    public class ReportController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
