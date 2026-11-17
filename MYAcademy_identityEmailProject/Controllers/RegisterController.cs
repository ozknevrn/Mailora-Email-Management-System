using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Models;
using MYAcademy_IdentityEmailProject.Services;

namespace MYAcademy_IdentityEmailProject.Controllers
{
    public class RegisterController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly VerificationCodeService _verificationService;

        public RegisterController(UserManager<AppUser> userManager, VerificationCodeService verificationService)
        {
            _userManager = userManager;
            _verificationService = verificationService;
        }

        [HttpGet]
        public IActionResult SignUp()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Inbox", "Message");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(UserRegisterViewModel userRegisterViewModel)
        {
           
            if (!ModelState.IsValid)
                return View(userRegisterViewModel);

            AppUser appUser = new AppUser()
            {
                Name = userRegisterViewModel.Name.Trim(),
                Surname = userRegisterViewModel.Surname.Trim(),
                Email = userRegisterViewModel.Email.Trim(),
                UserName = userRegisterViewModel.Username.Trim(),
                CreatedDate = DateTime.Now
            };

         
            VerificationCodeService.AssignNewCode(appUser);

            var result = await _userManager.CreateAsync(appUser, userRegisterViewModel.Password);

            if (result.Succeeded)
            {
               
                var sent = await _verificationService.SendCodeAsync(appUser);

                TempData["Mail"] = appUser.Email;
                TempData[sent ? "Success" : "Warning"] = sent
                    ? $"{appUser.Email} adresine 6 haneli doğrulama kodu gönderildi."
                    : "Hesabınız oluşturuldu ancak doğrulama e-postası gönderilemedi. \"Kodu tekrar gönder\" ile yeniden deneyin.";

              
                return RedirectToAction("Index", "ConfirmMail", new { email = appUser.Email });
            }

            foreach (var item in result.Errors)
            {
                ModelState.AddModelError("", item.Description);
            }
            return View(userRegisterViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> CheckUserName(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return Json(new { available = false });
            var user = await _userManager.FindByNameAsync(userName.Trim());
            return Json(new { available = user == null });
        }
    }
}
