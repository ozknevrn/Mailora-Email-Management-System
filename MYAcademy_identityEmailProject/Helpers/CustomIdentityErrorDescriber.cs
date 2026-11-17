using Microsoft.AspNetCore.Identity;

namespace MYAcademy_IdentityEmailProject.Helpers
{
    
    public class CustomIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError PasswordTooShort(int length) => new() { Code = nameof(PasswordTooShort), Description = $"Şifre en az {length} karakter olmalıdır." };
        public override IdentityError PasswordRequiresUpper() => new() { Code = nameof(PasswordRequiresUpper), Description = "Şifre en az bir büyük harf (A-Z) içermelidir." };
        public override IdentityError PasswordRequiresLower() => new() { Code = nameof(PasswordRequiresLower), Description = "Şifre en az bir küçük harf (a-z) içermelidir." };
        public override IdentityError PasswordRequiresDigit() => new() { Code = nameof(PasswordRequiresDigit), Description = "Şifre en az bir rakam (0-9) içermelidir." };
        public override IdentityError PasswordRequiresNonAlphanumeric() => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Şifre en az bir özel karakter (. ! * vb.) içermelidir." };
        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"Şifre en az {uniqueChars} farklı karakter içermelidir." };
        public override IdentityError PasswordMismatch() => new() { Code = nameof(PasswordMismatch), Description = "Mevcut şifreniz hatalı." };
        public override IdentityError DuplicateUserName(string userName) => new() { Code = nameof(DuplicateUserName), Description = $"'{userName}' kullanıcı adı zaten alınmış." };
        public override IdentityError DuplicateEmail(string email) => new() { Code = nameof(DuplicateEmail), Description = $"'{email}' e-posta adresiyle kayıtlı bir hesap zaten var." };
        public override IdentityError InvalidUserName(string? userName) => new() { Code = nameof(InvalidUserName), Description = $"'{userName}' geçersiz bir kullanıcı adı. Yalnızca harf, rakam ve . _ - kullanılabilir." };
        public override IdentityError InvalidEmail(string? email) => new() { Code = nameof(InvalidEmail), Description = $"'{email}' geçerli bir e-posta adresi değil." };
        public override IdentityError InvalidToken() => new() { Code = nameof(InvalidToken), Description = "Bağlantı geçersiz veya süresi dolmuş. Lütfen yeni bir bağlantı isteyin." };
        public override IdentityError UserLockoutNotEnabled() => new() { Code = nameof(UserLockoutNotEnabled), Description = "Bu kullanıcı için hesap kilitleme etkin değil." };
        public override IdentityError DefaultError() => new() { Code = nameof(DefaultError), Description = "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin." };
        public override IdentityError ConcurrencyFailure() => new() { Code = nameof(ConcurrencyFailure), Description = "Kayıt başka bir işlem tarafından değiştirildi, lütfen tekrar deneyin." };
    }
}
