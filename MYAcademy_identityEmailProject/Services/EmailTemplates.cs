using System.Net;

namespace MYAcademy_IdentityEmailProject.Services
{
    
    public static class EmailTemplates
    {
        private static string Layout(string title, string content) => $@"
<div style=""background:#F8F9FD;padding:32px 12px;font-family:Segoe UI,Arial,sans-serif;color:#303348"">
  <div style=""max-width:520px;margin:0 auto;background:#fff;border-radius:16px;overflow:hidden;box-shadow:0 8px 30px rgba(48,51,72,.08)"">
    <div style=""background:linear-gradient(135deg,#8B7FD6,#6F62C6);padding:22px 28px;color:#fff"">
      <div style=""font-size:20px;font-weight:800"">✉ Mail<span style=""opacity:.85"">ora</span></div>
      <div style=""font-size:13px;opacity:.9;margin-top:4px"">{WebUtility.HtmlEncode(title)}</div>
    </div>
    <div style=""padding:28px"">{content}</div>
    <div style=""padding:16px 28px;background:#F8F9FD;font-size:12px;color:#85899B"">Bu e-posta Mailora tarafından otomatik gönderilmiştir.</div>
  </div>
</div>";

        public static string VerificationCode(string name, int code, int minutes) => Layout("E-posta Doğrulama Kodu", $@"
<p style=""margin:0 0 12px"">Merhaba <b>{WebUtility.HtmlEncode(name)}</b>,</p>
<p style=""margin:0 0 18px"">Mailora hesabınızı etkinleştirmek için aşağıdaki 6 haneli doğrulama kodunu kullanın:</p>
<div style=""font-size:32px;font-weight:800;letter-spacing:10px;text-align:center;background:#F1F0FB;color:#5A4FB0;border-radius:12px;padding:18px 0"">{code}</div>
<p style=""margin:18px 0 0;font-size:13px;color:#85899B"">Kod <b>{minutes} dakika</b> geçerlidir. Bu işlemi siz yapmadıysanız bu e-postayı dikkate almayın.</p>");

        public static string PasswordReset(string name, string link, int minutes) => Layout("Şifre Sıfırlama", $@"
<p style=""margin:0 0 12px"">Merhaba <b>{WebUtility.HtmlEncode(name)}</b>,</p>
<p style=""margin:0 0 18px"">Hesabınız için bir şifre sıfırlama isteği aldık. Yeni şifrenizi belirlemek için butona tıklayın:</p>
<p style=""text-align:center;margin:0 0 18px""><a href=""{WebUtility.HtmlEncode(link)}"" style=""display:inline-block;background:#8B7FD6;color:#fff;text-decoration:none;font-weight:700;padding:12px 26px;border-radius:10px"">Şifremi Sıfırla</a></p>
<p style=""margin:0;font-size:13px;color:#85899B"">Bağlantı <b>{minutes} dakika</b> geçerlidir ve yalnızca <b>bir kez</b> kullanılabilir. Bu isteği siz yapmadıysanız şifreniz değişmez, e-postayı yok sayabilirsiniz.</p>");

        public static string Message(string senderName, string senderEmail, string subject, string sanitizedHtmlBody) => Layout(subject, $@"
<p style=""margin:0 0 16px;font-size:13px;color:#85899B""><b style=""color:#303348"">{WebUtility.HtmlEncode(senderName)}</b> &lt;{WebUtility.HtmlEncode(senderEmail)}&gt; size Mailora üzerinden bir mesaj gönderdi:</p>
<div style=""font-size:14.5px;line-height:1.65"">{sanitizedHtmlBody}</div>");
    }
}
