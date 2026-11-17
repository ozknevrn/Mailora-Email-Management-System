using Microsoft.AspNetCore.Mvc;

namespace MYAcademy_IdentityEmailProject.Controllers
{
    
    public class ErrorPagesController : Controller
    {
        public IActionResult Index(int code)
        {
            return code switch
            {
                401 => Page401(),
                403 => Page403(),
                _ => Page404()
            };
        }

        public IActionResult Page401()
        {
            Response.StatusCode = 401;
            return View("Page401");
        }

        public IActionResult Page403()
        {
            Response.StatusCode = 403;
            return View("Page403");
        }

        public IActionResult Page404()
        {
            Response.StatusCode = 404;
            return View("Page404");
        }
    }
}
