using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MYAcademy_IdentityEmailProject.Context;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Helpers;
using MYAcademy_IdentityEmailProject.Models;

namespace MYAcademy_IdentityEmailProject.Controllers
{
    
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DashboardController(EmailContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = (await _userManager.GetUserAsync(User))!;
            var email = user.Email!;
            var today = DateTime.Today;
            var start = today.AddDays(-13);   

            var inbox = _context.Messages.AsNoTracking().InboxOf(email);
            var sent = _context.Messages.AsNoTracking().SentOf(email);

            var vm = new DashboardViewModel
            {
                FirstName = string.IsNullOrWhiteSpace(user.Name) ? user.UserName! : user.Name,
                InboxCount = await inbox.CountAsync(),
                UnreadCount = await inbox.CountAsync(m => !m.IsRead),
                SentCount = await sent.CountAsync(),
                StarredCount = await _context.Messages.StarredOf(email).CountAsync(),
                DraftCount = await _context.Messages.DraftsOf(email).CountAsync(),
                TrashCount = await _context.Messages.TrashOf(email).CountAsync(),
                TodayReceived = await inbox.CountAsync(m => m.SendDate >= today),
                WeekSent = await sent.CountAsync(m => m.SendDate >= today.AddDays(-6))
            };

          
            var recent = await inbox.Include(m => m.Category).OrderByDescending(m => m.SendDate).Take(5).ToListAsync();
            var senderEmails = recent.Select(m => m.SenderEmail.ToUpperInvariant()).Distinct().ToList();
            var names = await _userManager.Users.AsNoTracking()
                .Where(u => u.NormalizedEmail != null && senderEmails.Contains(u.NormalizedEmail))
                .Select(u => new { u.Email, u.Name, u.Surname, u.UserName }).ToListAsync();
            vm.RecentMessages = recent.Select(m =>
            {
                var u = names.FirstOrDefault(n => string.Equals(n.Email, m.SenderEmail, StringComparison.OrdinalIgnoreCase));
                return new MessageListItem
                {
                    Id = m.MessageId,
                    IsIncoming = true,
                    PersonEmail = m.SenderEmail,
                    PersonName = u != null ? MailoraUi.FullName(u.Name, u.Surname, u.UserName) : m.SenderEmail,
                    Subject = m.Subject,
                    Snippet = HtmlSanitizer.Snippet(m.MessageDetail, 90),
                    Date = m.SendDate,
                    IsRead = m.IsRead,
                    CategoryName = m.Category?.CategoryName ?? "Genel",
                    CategoryColor = m.Category?.Color
                };
            }).ToList();

            
            var mine = _context.Messages.AsNoTracking().Where(m => !m.IsDraft &&
                ((m.ReceiverEmail == email && !m.ReceiverIsDeleted && !m.ReceiverIsPermanentlyDeleted) ||
                 (m.SenderEmail == email && !m.SenderIsDeleted && !m.SenderIsPermanentlyDeleted)));
            var dist = await mine.GroupBy(m => m.CategoryId).Select(g => new { g.Key, Count = g.Count() }).ToListAsync();
            var cats = await _context.Categories.AsNoTracking().ToListAsync();
            vm.CategoryDistribution = cats
                .Select(c => new CategoryOption { Id = c.CategoryId, Name = c.CategoryName, Color = c.Color ?? "#8B7FD6", Count = dist.FirstOrDefault(d => d.Key == c.CategoryId)?.Count ?? 0 })
                .Where(c => c.Count > 0)
                .OrderByDescending(c => c.Count)
                .ToList();

            
            var incomingDates = await inbox.Where(m => m.SendDate >= start).Select(m => m.SendDate).ToListAsync();
            var sentDates = await sent.Where(m => m.SendDate >= start).Select(m => m.SendDate).ToListAsync();
            for (var d = start; d <= today; d = d.AddDays(1))
            {
                var day = d;
                vm.TrafficLabels.Add(day.ToString("d MMM", MailoraUi.Tr));
                vm.TrafficIncoming.Add(incomingDates.Count(x => x.Date == day));
                vm.TrafficSent.Add(sentDates.Count(x => x.Date == day));
            }

            
            var contacts = await mine.Select(m => m.ReceiverEmail == email ? m.SenderEmail : m.ReceiverEmail).ToListAsync();
            var top = contacts.Where(e => !string.IsNullOrEmpty(e) && !string.Equals(e, email, StringComparison.OrdinalIgnoreCase))
                .GroupBy(e => e.ToLowerInvariant())
                .Select(g => new { Email = g.Key, Total = g.Count() })
                .OrderByDescending(x => x.Total).Take(5).ToList();
            var topEmails = top.Select(t => t.Email.ToUpperInvariant()).ToList();
            var topUsers = await _userManager.Users.AsNoTracking()
                .Where(u => u.NormalizedEmail != null && topEmails.Contains(u.NormalizedEmail))
                .Select(u => new { u.Email, u.Name, u.Surname, u.UserName }).ToListAsync();
            vm.TopContacts = top.Select(t =>
            {
                var u = topUsers.FirstOrDefault(x => string.Equals(x.Email, t.Email, StringComparison.OrdinalIgnoreCase));
                return new ContactStat { Email = t.Email, Total = t.Total, Name = u != null ? MailoraUi.FullName(u.Name, u.Surname, u.UserName) : t.Email };
            }).ToList();

            ViewData["ActiveMenu"] = "dashboard";
            return View(vm);
        }
    }
}
