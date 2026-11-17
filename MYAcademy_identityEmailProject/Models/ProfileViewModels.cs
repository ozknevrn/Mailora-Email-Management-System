using System.ComponentModel.DataAnnotations;

namespace MYAcademy_IdentityEmailProject.Models
{
    public class ProfileUpdateViewModel
    {
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Ad 2-50 karakter olmalıdır.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Soyad 2-50 karakter olmalıdır.")]
        public string Surname { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "Hakkımda en fazla 300 karakter olabilir.")]
        public string? Description { get; set; }

        [Phone(ErrorMessage = "Geçerli bir telefon numarası girin.")]
        public string? PhoneNumber { get; set; }

        public IFormFile? Photo { get; set; }
        public bool RemovePhoto { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

   
    public class ProfileViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime? CreatedDate { get; set; }

        public int InboxCount { get; set; }
        public int SentCount { get; set; }
        public int StarredCount { get; set; }
        public int DraftCount { get; set; }

        public ProfileUpdateViewModel Info { get; set; } = new();
        public ChangePasswordViewModel Password { get; set; } = new();
    }
}
