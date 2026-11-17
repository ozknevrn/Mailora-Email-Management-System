namespace MYAcademy_IdentityEmailProject.Services
{
    public interface IMailService
    {
        Task SendAsync(string toEmail, string toName, string subject, string htmlBody, string? replyToEmail = null, string? replyToName = null);
    }
}
