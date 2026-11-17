using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Models;
using MYAcademy_IdentityEmailProject.Services;

namespace MYAcademy_IdentityEmailProject.Controllers
{
    // Kayıttan sonra gelen 6 haneli kodun girildiği "mail aktivasyon" sayfası
    public class ConfirmMailController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly VerificationCodeService _verificationService;

        public ConfirmMailController(UserManager<AppUser> userManager, VerificationCodeService verificationService)
        {
            _userManager = userManager;
            _verificationService = verificationService;
        }

        [HttpGet]
        public IActionResult Index(string? email)
        {

            var mail = email ?? TempData["Mail"] as string ?? string.Empty;
            return View(new ConfirmMailViewModel { Email = mail });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ConfirmMailViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user == null)
            {
                ModelState.AddModelError("", "Kod hatalı veya bu e-posta ile kayıtlı bir hesap yok.");
                return View(model);
            }

            if (user.EmailConfirmed)
            {
                TempData["Success"] = "E-posta adresiniz zaten doğrulanmış. Giriş yapabilirsiniz.";
                return RedirectToAction("SignIn", "Login");
            }

            if (user.ValidCodeExpireDate != null && user.ValidCodeExpireDate < DateTime.Now)
            {
                ModelState.AddModelError("", "Doğrulama kodunun süresi doldu. \"Kodu tekrar gönder\" ile yeni kod isteyin.");
                return View(model);
            }

            if (user.ValidCode == null || user.ValidCode.ToString() != model.Code.Trim())
            {
                
                await _userManager.AccessFailedAsync(user);
                if (await _userManager.IsLockedOutAsync(user))
                {
                    ModelState.AddModelError("", "Çok fazla hatalı deneme yapıldı. Lütfen birkaç dakika sonra tekrar deneyin.");
                    return View(model);
                }
                ModelState.AddModelError(nameof(model.Code), "Doğrulama kodu hatalı.");
                return View(model);
            }

            
            user.EmailConfirmed = true;
            user.ValidCode = null;
            user.ValidCodeExpireDate = null;
            await _userManager.UpdateAsync(user);
            await _userManager.ResetAccessFailedCountAsync(user);

            TempData["Success"] = "E-posta adresiniz doğrulandı. Artık giriş yapabilirsiniz.";
            return RedirectToAction("SignIn", "Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resend(string email)
        {
            var user = string.IsNullOrWhiteSpace(email) ? null : await _userManager.FindByEmailAsync(email.Trim());

            if (user != null && !user.EmailConfirmed)
            {
                var wait = VerificationCodeService.SecondsUntilResend(user);
                if (wait > 0)
                {
                    TempData["Warning"] = $"Yeni kod isteyebilmek için {wait} saniye bekleyin.";
                }
                else
                {
                    var sent = await _verificationService.RegenerateAndSendAsync(user);
                    TempData[sent ? "Success" : "Warning"] = sent
                        ? "Yeni doğrulama kodu e-posta adresinize gönderildi."
                        : "E-posta gönderilemedi. Mail ayarlarını (appsettings.Secrets.json) kontrol edin.";
                }
            }
            else
            {
                // Hesap var mı yok mu bilgisini dışarı sızdırmamak için aynı mesaj
                TempData["Success"] = "Bu adrese ait doğrulanmamış bir hesap varsa yeni kod gönderildi.";
            }

            return RedirectToAction(nameof(Index), new { email });
        }
    }
}
