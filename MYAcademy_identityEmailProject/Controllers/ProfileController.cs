using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MYAcademy_IdentityEmailProject.Context;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Helpers;
using MYAcademy_IdentityEmailProject.Models;
using MYAcademy_IdentityEmailProject.Services;

namespace MYAcademy_IdentityEmailProject.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly EmailContext _context;
        private readonly IImageUploadService _imageService;

        public ProfileController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, EmailContext context, IImageUploadService imageService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _imageService = imageService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = (await _userManager.GetUserAsync(User))!;
            return View(await BuildAsync(user));
        }

        // Ad, soyad, hakkımda, telefon ve profil fotoğrafı
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInfo([Bind(Prefix = "Info")] ProfileUpdateViewModel model)
        {
            var user = (await _userManager.GetUserAsync(User))!;

            if (model.Photo != null)
            {
                var photoError = _imageService.Validate(model.Photo);
                if (photoError != null) ModelState.AddModelError("Info.Photo", photoError);
            }

            if (!ModelState.IsValid)
            {
                var vm = await BuildAsync(user);
                vm.Info = model;
                ViewData["OpenTab"] = "info";
                return View(nameof(Index), vm);
            }

            user.Name = model.Name.Trim();
            user.Surname = model.Surname.Trim();
            user.Description = model.Description?.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();

            if (model.RemovePhoto && model.Photo == null)
            {
                _imageService.Delete(user.ImageUrl);
                user.ImageUrl = null;
            }
            else if (model.Photo != null)
            {
                var old = user.ImageUrl;
                user.ImageUrl = await _imageService.SaveAsync(model.Photo, "profiles");
                _imageService.Delete(old);
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                var vm = await BuildAsync(user);
                vm.Info = model;
                return View(nameof(Index), vm);
            }

            TempData["Toast"] = "Profil bilgileriniz güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        // Mevcut şifreyi değiştirme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] ChangePasswordViewModel model)
        {
            var user = (await _userManager.GetUserAsync(User))!;

            if (ModelState.IsValid)
            {
                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (result.Succeeded)
                {
                    // Şifre değişince SecurityStamp değişir; oturum çerezini yenileyerek kullanıcıyı açık tutuyoruz
                    await _signInManager.RefreshSignInAsync(user);
                    TempData["Toast"] = "Şifreniz başarıyla değiştirildi.";
                    return RedirectToAction(nameof(Index));
                }
                foreach (var e in result.Errors)
                    ModelState.AddModelError(e.Code == nameof(IdentityErrorDescriber.PasswordMismatch) ? "Password.CurrentPassword" : "Password.NewPassword", e.Description);
            }

            var vm = await BuildAsync(user);
            ViewData["OpenTab"] = "password";
            return View(nameof(Index), vm);
        }

        private async Task<ProfileViewModel> BuildAsync(AppUser user)
        {
            var email = user.Email!;
            return new ProfileViewModel
            {
                UserId = user.Id,
                UserName = user.UserName!,
                Email = email,
                FullName = MailoraUi.FullName(user.Name, user.Surname, user.UserName),
                ImageUrl = user.ImageUrl,
                EmailConfirmed = user.EmailConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                CreatedDate = user.CreatedDate,
                InboxCount = await _context.Messages.InboxOf(email).CountAsync(),
                SentCount = await _context.Messages.SentOf(email).CountAsync(),
                StarredCount = await _context.Messages.StarredOf(email).CountAsync(),
                DraftCount = await _context.Messages.DraftsOf(email).CountAsync(),
                Info = new ProfileUpdateViewModel
                {
                    Name = user.Name ?? "",
                    Surname = user.Surname ?? "",
                    Description = user.Description,
                    PhoneNumber = user.PhoneNumber
                }
            };
        }
    }
}
