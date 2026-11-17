using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Helpers;
using MYAcademy_IdentityEmailProject.Models;
using MYAcademy_IdentityEmailProject.Services;

namespace MYAcademy_IdentityEmailProject.Controllers
{

    public class PasswordController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IMailService _mailService;
        private readonly ILogger<PasswordController> _logger;

        public PasswordController(UserManager<AppUser> userManager, IMailService mailService, ILogger<PasswordController> logger)
        {
            _userManager = userManager;
            _mailService = mailService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user != null && user.EmailConfirmed)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                // Token içinde + / = gibi karakterler olabilir; URL'de bozulmaması için Base64Url'e çevrilir
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var link = Url.Action(nameof(ResetPassword), "Password", new { userId = user.Id, token = encodedToken }, Request.Scheme)!;

                try
                {
                    var name = MailoraUi.FullName(user.Name, user.Surname, user.UserName);
                    await _mailService.SendAsync(user.Email!, name, "Mailora · Şifre Sıfırlama", EmailTemplates.PasswordReset(name, link, 30));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Şifre sıfırlama e-postası gönderilemedi");
                    ModelState.AddModelError("", "E-posta gönderilemedi. Lütfen daha sonra tekrar deneyin.");
                    return View(model);
                }
            }

            // Hesap olsun olmasın AYNI mesaj gösterilir: böylece kimse hangi e-postaların kayıtlı olduğunu öğrenemez
            TempData["Success"] = "Bu e-posta adresi kayıtlıysa şifre sıfırlama bağlantısı gönderildi. Gelen kutunuzu (ve spam klasörünü) kontrol edin.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string? userId, string? token)
        {
            var model = new ResetPasswordViewModel { UserId = userId ?? "", Token = token ?? "" };
            model.IsTokenInvalid = !await IsTokenValidAsync(userId, token);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            var user = string.IsNullOrEmpty(model.UserId) ? null : await _userManager.FindByIdAsync(model.UserId);
            var rawToken = DecodeToken(model.Token);
            if (user == null || rawToken == null)
            {
                model.IsTokenInvalid = true;
                return View(model);
            }

            if (!ModelState.IsValid) return View(model);

            var result = await _userManager.ResetPasswordAsync(user, rawToken, model.NewPassword);
            if (result.Succeeded)
            {
                // Şifresini sıfırlayan kullanıcının kilidi de açılır
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);
                TempData["Success"] = "Şifreniz başarıyla güncellendi. Yeni şifrenizle giriş yapabilirsiniz.";
                return RedirectToAction("SignIn", "Login");
            }

            if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
            {
                model.IsTokenInvalid = true;
                return View(model);
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            return View(model);
        }

        private async Task<bool> IsTokenValidAsync(string? userId, string? token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            var raw = DecodeToken(token);
            if (user == null || raw == null) return false;

            // Token'ı kullanmadan geçerli mi diye kontrol et (süresi dolmuş / kullanılmış bağlantıyı baştan yakalar)
            return await _userManager.VerifyUserTokenAsync(user, _userManager.Options.Tokens.PasswordResetTokenProvider,
                UserManager<AppUser>.ResetPasswordTokenPurpose, raw);
        }

        private static string? DecodeToken(string? token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            try { return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token)); }
            catch (FormatException) { return null; }
        }
    }
}
