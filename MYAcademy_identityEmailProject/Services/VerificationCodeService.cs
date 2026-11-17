using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Helpers;

namespace MYAcademy_IdentityEmailProject.Services
{
   
    public class VerificationCodeService
    {
        public const int CodeLifetimeMinutes = 10;
        public const int ResendWaitSeconds = 60;

        private readonly UserManager<AppUser> _userManager;
        private readonly IMailService _mailService;
        private readonly ILogger<VerificationCodeService> _logger;

        public VerificationCodeService(UserManager<AppUser> userManager, IMailService mailService, ILogger<VerificationCodeService> logger)
        {
            _userManager = userManager;
            _mailService = mailService;
            _logger = logger;
        }

        
        public static int NewCode() => RandomNumberGenerator.GetInt32(100000, 1000000);

      
        public static void AssignNewCode(AppUser user)
        {
            user.ValidCode = NewCode();
            user.ValidCodeExpireDate = DateTime.Now.AddMinutes(CodeLifetimeMinutes);
        }

        public static int SecondsUntilResend(AppUser user)
        {
            if (user.ValidCodeExpireDate == null) return 0;
            var issuedAt = user.ValidCodeExpireDate.Value.AddMinutes(-CodeLifetimeMinutes);
            var wait = issuedAt.AddSeconds(ResendWaitSeconds) - DateTime.Now;
            return wait.TotalSeconds > 0 ? (int)Math.Ceiling(wait.TotalSeconds) : 0;
        }

       
        public async Task<bool> SendCodeAsync(AppUser user)
        {
            if (user.ValidCode == null || string.IsNullOrEmpty(user.Email)) return false;
            try
            {
                var name = MailoraUi.FullName(user.Name, user.Surname, user.UserName);
                await _mailService.SendAsync(user.Email, name, "Mailora · E-posta Doğrulama Kodu",
                    EmailTemplates.VerificationCode(name, user.ValidCode.Value, CodeLifetimeMinutes));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Doğrulama kodu gönderilemedi: {Email}", user.Email);
                return false;
            }
        }

      
        public async Task<bool> RegenerateAndSendAsync(AppUser user)
        {
            AssignNewCode(user);
            await _userManager.UpdateAsync(user);
            return await SendCodeAsync(user);
        }
    }
}
