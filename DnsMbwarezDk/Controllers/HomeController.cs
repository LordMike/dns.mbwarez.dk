using System.Web.Mvc;

namespace DnsMbwarezDk.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Data");

            ViewBag.Title = "Home Page";

            return View();
        }
    }
}
