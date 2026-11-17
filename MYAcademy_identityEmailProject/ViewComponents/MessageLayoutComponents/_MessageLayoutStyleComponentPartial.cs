using Microsoft.AspNetCore.Mvc;

namespace MYAcademy_IdentityEmailProject.ViewComponents.MessageLayoutComponents
{
    public class _MessageLayoutStyleComponentPartial : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
