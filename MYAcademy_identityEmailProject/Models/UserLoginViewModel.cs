using System.ComponentModel.DataAnnotations;

namespace MYAcademy_IdentityEmailProject.Models
{
    public class UserLoginViewModel
    {
        // Kullanıcı adı VEYA e-posta ile giriş yapılabilir
        [Required(ErrorMessage = "Kullanıcı adı veya e-posta zorunludur.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        public string Password { get; set; } = string.Empty;

        // "Beni hatırla": tarayıcı kapansa da oturum açık kalır
        public bool RememberMe { get; set; }
    }
}
