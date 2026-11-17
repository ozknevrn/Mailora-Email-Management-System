using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MYAcademy_IdentityEmailProject.Context;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Helpers;

namespace MYAcademy_IdentityEmailProject.ViewComponents.MessageLayoutComponents
{
   
    public class _MessageLayoutHeaderComponentPartial : ViewComponent
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;

        public _MessageLayoutHeaderComponentPartial(EmailContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = new HeaderViewModel();
            var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
            if (user != null)
            {
                model.FullName = MailoraUi.FullName(user.Name, user.Surname, user.UserName);
                model.UserName = user.UserName ?? "";
                model.Email = user.Email ?? "";
                model.ImageUrl = user.ImageUrl;

                if (user.Email != null)
                {
                    var inbox = _context.Messages.AsNoTracking().InboxOf(user.Email).Where(m => !m.IsRead);
                    model.UnreadCount = await inbox.CountAsync();
                    var latest = await inbox.OrderByDescending(m => m.SendDate).Take(5)
                        .Select(m => new { m.MessageId, m.SenderEmail, m.Subject, m.SendDate }).ToListAsync();

                    var emails = latest.Select(l => l.SenderEmail.ToUpperInvariant()).Distinct().ToList();
                    var senders = await _userManager.Users.AsNoTracking()
                        .Where(u => u.NormalizedEmail != null && emails.Contains(u.NormalizedEmail))
                        .Select(u => new { u.Email, u.Name, u.Surname, u.UserName }).ToListAsync();

                    model.Notifications = latest.Select(l =>
                    {
                        var s = senders.FirstOrDefault(x => string.Equals(x.Email, l.SenderEmail, StringComparison.OrdinalIgnoreCase));
                        return new HeaderNotification
                        {
                            MessageId = l.MessageId,
                            SenderName = s != null ? MailoraUi.FullName(s.Name, s.Surname, s.UserName) : l.SenderEmail,
                            SenderEmail = l.SenderEmail,
                            Subject = l.Subject,
                            Date = l.SendDate
                        };
                    }).ToList();
                }
            }
            return View(model);
        }
    }

    public class HeaderViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int UnreadCount { get; set; }
        public List<HeaderNotification> Notifications { get; set; } = new();
    }

    public class HeaderNotification
    {
        public int MessageId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}
