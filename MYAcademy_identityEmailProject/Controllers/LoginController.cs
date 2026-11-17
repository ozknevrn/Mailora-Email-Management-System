using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Models;

namespace MYAcademy_IdentityEmailProject.Controllers
{
    public class LoginController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        public LoginController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult SignIn(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Inbox", "Message");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(UserLoginViewModel userLoginViewModel, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid) return View(userLoginViewModel);

            
            var login = userLoginViewModel.Username.Trim();
            var user = login.Contains('@')
                ? await _userManager.FindByEmailAsync(login)
                : await _userManager.FindByNameAsync(login);

            if (user == null)
            {
                ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı.");
                return View(userLoginViewModel);
            }

            var result = await _signInManager.PasswordSignInAsync(user, userLoginViewModel.Password,
                userLoginViewModel.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
          
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Inbox", "Message");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Çok fazla hatalı deneme yaptınız. Hesabınız 5 dakika kilitlendi.");
                return View(userLoginViewModel);
            }

          
            if (result.IsNotAllowed && !user.EmailConfirmed && await _userManager.CheckPasswordAsync(user, userLoginViewModel.Password))
            {
                TempData["Warning"] = "Giriş yapabilmek için önce e-posta adresinizi doğrulamalısınız. Kod gelmediyse \"Kodu tekrar gönder\" butonunu kullanın.";
                return RedirectToAction("Index", "ConfirmMail", new { email = user.Email });
            }

            ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı.");
            return View(userLoginViewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["Success"] = "Güvenli bir şekilde çıkış yaptınız.";
            return RedirectToAction(nameof(SignIn));
        }
    }
}
