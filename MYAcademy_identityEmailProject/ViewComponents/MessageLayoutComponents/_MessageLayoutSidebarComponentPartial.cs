using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MYAcademy_IdentityEmailProject.Context;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Helpers;

namespace MYAcademy_IdentityEmailProject.ViewComponents.MessageLayoutComponents
{
    
    public class _MessageLayoutSidebarComponentPartial : ViewComponent
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;

        public _MessageLayoutSidebarComponentPartial(EmailContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(string? active = null)
        {
            var model = new SidebarViewModel { Active = active ?? "inbox" };
            var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
            if (user?.Email != null)
            {
                var email = user.Email;
                model.Unread = await _context.Messages.InboxOf(email).CountAsync(m => !m.IsRead);
                model.Starred = await _context.Messages.StarredOf(email).CountAsync();
                model.Drafts = await _context.Messages.DraftsOf(email).CountAsync();
                model.Trash = await _context.Messages.TrashOf(email).CountAsync();
                model.Sent = await _context.Messages.SentOf(email).CountAsync();
            }
            model.Categories = await _context.Categories.AsNoTracking().OrderBy(c => c.CategoryName)
                .Select(c => new SidebarCategory { Id = c.CategoryId, Name = c.CategoryName, Color = c.Color ?? "#8B7FD6" })
                .ToListAsync();
            return View(model);
        }
    }

    public class SidebarViewModel
    {
        public string Active { get; set; } = "inbox";
        public int Unread { get; set; }
        public int Starred { get; set; }
        public int Drafts { get; set; }
        public int Trash { get; set; }
        public int Sent { get; set; }
        public List<SidebarCategory> Categories { get; set; } = new();
    }

    public class SidebarCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#8B7FD6";
    }
}
