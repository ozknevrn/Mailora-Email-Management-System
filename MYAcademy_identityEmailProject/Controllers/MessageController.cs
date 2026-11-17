using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MYAcademy_IdentityEmailProject.Context;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Helpers;
using MYAcademy_IdentityEmailProject.Models;
using MYAcademy_IdentityEmailProject.Services;

namespace MYAcademy_IdentityEmailProject.Controllers
{

    [Authorize]
    public class MessageController : Controller
    {
        private const int PageSize = 10;

        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMailService _mailService;
        private readonly ILogger<MessageController> _logger;

        public MessageController(EmailContext context, UserManager<AppUser> userManager, IMailService mailService, ILogger<MessageController> logger)
        {
            _context = context;
            _userManager = userManager;
            _mailService = mailService;
            _logger = logger;
        }

        private async Task<AppUser> CurrentUserAsync() =>
            (await _userManager.GetUserAsync(User))!;   // [Authorize] olduğu için null gelmez

  
        public Task<IActionResult> Inbox(string? q, int? categoryId, string? filter, string? sort, int page = 1) =>
            ListBox("inbox", q, categoryId, filter, sort, page);

        public Task<IActionResult> Sendbox(string? q, int? categoryId, string? filter, string? sort, int page = 1) =>
            ListBox("sent", q, categoryId, filter, sort, page);

        public Task<IActionResult> Starred(string? q, int? categoryId, string? filter, string? sort, int page = 1) =>
            ListBox("starred", q, categoryId, filter, sort, page);

        public Task<IActionResult> Drafts(string? q, int? categoryId, string? filter, string? sort, int page = 1) =>
            ListBox("drafts", q, categoryId, filter, sort, page);

        public Task<IActionResult> Trash(string? q, int? categoryId, string? filter, string? sort, int page = 1) =>
            ListBox("trash", q, categoryId, filter, sort, page);

        private async Task<IActionResult> ListBox(string box, string? q, int? categoryId, string? filter, string? sort, int page)
        {
            var user = await CurrentUserAsync();
            var email = user.Email!;

            var boxQuery = _context.Messages.AsNoTracking().ForBox(box, email);

            var vm = new MessageListViewModel
            {
                Box = box,
                UserEmail = email,
                Query = q?.Trim(),
                CategoryId = categoryId,
                Sort = sort == "asc" ? "asc" : "desc",
                Filter = filter is "unread" or "starred" ? filter : "all",
                PageSize = PageSize
            };
            (vm.Title, vm.Icon, vm.EmptyText) = box switch
            {
                "sent" => ("Gönderilenler", "bi-send", "Henüz mesaj göndermediniz."),
                "starred" => ("Yıldızlı Mesajlar", "bi-star", "Önemli mesajlarınızı yıldızlayarak burada toplayabilirsiniz."),
                "drafts" => ("Taslaklar", "bi-file-earmark-text", "Kaydedilmiş taslağınız yok."),
                "trash" => ("Çöp Kutusu", "bi-trash3", "Çöp kutunuz boş."),
                _ => ("Gelen Kutusu", "bi-inbox", "Yeni mesajlar geldiğinde burada listelenecek.")
            };

            vm.BoxTotal = await boxQuery.CountAsync();
            vm.UnreadCount = await _context.Messages.InboxOf(email).CountAsync(m => !m.IsRead);
            vm.StarredCount = await boxQuery.CountAsync(m =>
                (m.ReceiverEmail == email && m.IsFavorite) || (m.SenderEmail == email && m.SenderIsFavorite));

         
            var counts = await boxQuery.GroupBy(m => m.CategoryId).Select(g => new { g.Key, Count = g.Count() }).ToListAsync();
            vm.Categories = (await _context.Categories.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync())
                .Select(c => new CategoryOption { Id = c.CategoryId, Name = c.CategoryName, Color = c.Color, Count = counts.FirstOrDefault(x => x.Key == c.CategoryId)?.Count ?? 0 })
                .ToList();

       
            var query = boxQuery;
            if (!string.IsNullOrWhiteSpace(vm.Query))
            {
                var term = vm.Query;
                query = query.Where(m => m.Subject.Contains(term) || m.MessageDetail.Contains(term) ||
                                         m.SenderEmail.Contains(term) || m.ReceiverEmail.Contains(term));
            }
            if (vm.CategoryId.HasValue)
                query = query.Where(m => m.CategoryId == vm.CategoryId.Value);
            if (vm.Filter == "unread")
                query = query.Where(m => m.ReceiverEmail == email && !m.IsRead);
            if (vm.Filter == "starred")
                query = query.Where(m => (m.ReceiverEmail == email && m.IsFavorite) || (m.SenderEmail == email && m.SenderIsFavorite));

       
            query = vm.Sort == "asc" ? query.OrderBy(m => m.SendDate) : query.OrderByDescending(m => m.SendDate);

         
            vm.TotalCount = await query.CountAsync();
            vm.Page = Math.Clamp(page, 1, vm.TotalPages);
            var messages = await query.Include(m => m.Category)
                .Skip((vm.Page - 1) * PageSize).Take(PageSize).ToListAsync();

            vm.Items = await ToListItemsAsync(messages, box, email);
            ViewData["ActiveMenu"] = box;
            return View("MessageList", vm);
        }

        public async Task<IActionResult> MessageDetail(int id, string? box)
        {
            var user = await CurrentUserAsync();
            var email = user.Email!;

            var exists = await _context.Messages.AnyAsync(m => m.MessageId == id);
            if (!exists) return NotFound();                                    

            var message = await _context.Messages.Include(m => m.Category)
                .VisibleTo(email).FirstOrDefaultAsync(m => m.MessageId == id);
            if (message == null) return Forbid();                            

            if (message.IsDraft) return RedirectToAction(nameof(CreateMessage), new { draftId = id });

            bool incoming = message.ReceiverEmail == email && !message.ReceiverIsPermanentlyDeleted &&
                            (box != "sent" || message.SenderEmail != email);
            box ??= incoming ? (message.ReceiverIsDeleted ? "trash" : "inbox") : (message.SenderIsDeleted ? "trash" : "sent");

            // Alıcı açtıysa okundu olarak işaretle
            if (incoming && !message.IsRead)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            var people = await LookupPeopleAsync(new[] { message.SenderEmail, message.ReceiverEmail });
            var sender = people.GetValueOrDefault(message.SenderEmail.ToLowerInvariant());
            var receiver = people.GetValueOrDefault(message.ReceiverEmail.ToLowerInvariant());

            // Önceki / sonraki mesaj (aynı kutu içinde, yeniden eskiye)
            var ids = await _context.Messages.ForBox(box, email).OrderByDescending(m => m.SendDate).Select(m => m.MessageId).ToListAsync();
            var idx = ids.IndexOf(id);

            var vm = new MessageDetailViewModel
            {
                Id = message.MessageId,
                Subject = message.Subject,
                SafeHtmlBody = HtmlSanitizer.Sanitize(LooksLikeHtml(message.MessageDetail) ? message.MessageDetail : TextToHtml(message.MessageDetail)),
                SendDate = message.SendDate,
                CategoryId = message.CategoryId,
                CategoryName = message.Category?.CategoryName ?? "Genel",
                CategoryColor = message.Category?.Color,
                SenderEmail = message.SenderEmail,
                SenderName = sender?.Name ?? message.SenderEmail,
                SenderImageUrl = sender?.ImageUrl,
                ReceiverEmail = message.ReceiverEmail,
                ReceiverName = receiver?.Name ?? message.ReceiverEmail,
                ReceiverIsRegistered = receiver != null,
                IsIncoming = incoming,
                IsStarred = incoming ? message.IsFavorite : message.SenderIsFavorite,
                IsInTrash = incoming ? message.ReceiverIsDeleted : message.SenderIsDeleted,
                BackBox = box,
                PrevId = idx > 0 ? ids[idx - 1] : null,
                NextId = idx > -1 && idx < ids.Count - 1 ? ids[idx + 1] : null
            };
            ViewData["ActiveMenu"] = box;
            return View(vm);
        }

       
        [HttpGet]
        public async Task<IActionResult> CreateMessage(int? replyTo, int? forwardId, int? draftId, string? to)
        {
            var user = await CurrentUserAsync();
            var email = user.Email!;
            var vm = new ComposeMessageViewModel { ReceiverEmail = to ?? string.Empty };

            if (draftId.HasValue)
            {
                var draft = await _context.Messages.DraftsOf(email).FirstOrDefaultAsync(m => m.MessageId == draftId);
                if (draft == null) return NotFound();
                vm.DraftId = draft.MessageId;
                vm.ReceiverEmail = draft.ReceiverEmail;
                vm.Subject = draft.Subject;
                vm.MessageDetail = draft.MessageDetail;
                vm.CategoryId = draft.CategoryId;
                vm.Mode = "draft";
            }
            else if (replyTo.HasValue || forwardId.HasValue)
            {
                var sourceId = replyTo ?? forwardId!.Value;
                var source = await _context.Messages.VisibleTo(email).FirstOrDefaultAsync(m => m.MessageId == sourceId && !m.IsDraft);
                if (source == null) return Forbid();

                var people = await LookupPeopleAsync(new[] { source.SenderEmail });
                var senderName = people.GetValueOrDefault(source.SenderEmail.ToLowerInvariant())?.Name ?? source.SenderEmail;
                var original = LooksLikeHtml(source.MessageDetail) ? HtmlSanitizer.Sanitize(source.MessageDetail) : TextToHtml(source.MessageDetail);
                var date = MailoraUi.LongDate(source.SendDate);
                vm.CategoryId = source.CategoryId;

                if (replyTo.HasValue)
                {
                    // Yanıt, karşı tarafa gider (gelen mesajda gönderene, gönderilen mesajda alıcıya)
                    vm.ReceiverEmail = source.ReceiverEmail == email ? source.SenderEmail : source.ReceiverEmail;
                    vm.Subject = source.Subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase) ? source.Subject : "Re: " + source.Subject;
                    vm.MessageDetail = $"<p><br></p><p>{WebUtility.HtmlEncode(date)} tarihinde {WebUtility.HtmlEncode(senderName)} &lt;{WebUtility.HtmlEncode(source.SenderEmail)}&gt; şunu yazdı:</p><blockquote>{original}</blockquote>";
                    vm.Mode = "reply";
                }
                else
                {
                    vm.Subject = source.Subject.StartsWith("Fwd:", StringComparison.OrdinalIgnoreCase) ? source.Subject : "Fwd: " + source.Subject;
                    vm.MessageDetail = $"<p><br></p><p>---------- İletilen mesaj ----------<br>Kimden: {WebUtility.HtmlEncode(senderName)} &lt;{WebUtility.HtmlEncode(source.SenderEmail)}&gt;<br>Tarih: {WebUtility.HtmlEncode(date)}<br>Konu: {WebUtility.HtmlEncode(source.Subject)}<br>Kime: {WebUtility.HtmlEncode(source.ReceiverEmail)}</p><blockquote>{original}</blockquote>";
                    vm.Mode = "forward";
                }
            }

            await FillCategoriesAsync(vm);
            ViewData["ActiveMenu"] = "compose";
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMessage(ComposeMessageViewModel model, string? submitAction)
        {
            var user = await CurrentUserAsync();
            var email = user.Email!;
            var senderName = MailoraUi.FullName(user.Name, user.Surname, user.UserName);
            var cleanBody = HtmlSanitizer.Sanitize(model.MessageDetail);

            // Düzenlenen taslak gerçekten bu kullanıcıya mı ait?
            Message? draft = null;
            if (model.DraftId.HasValue)
            {
                draft = await _context.Messages.DraftsOf(email).FirstOrDefaultAsync(m => m.MessageId == model.DraftId);
                if (draft == null) return Forbid();
            }

       
            if (submitAction == "draft")
            {
                ModelState.Clear();   // taslakta alanlar boş olabilir
                var categoryId = await ValidCategoryIdOrDefaultAsync(model.CategoryId);
                draft ??= new Message { SenderEmail = email, IsDraft = true, IsRead = true };
                draft.ReceiverEmail = model.ReceiverEmail?.Trim() ?? string.Empty;
                draft.Subject = string.IsNullOrWhiteSpace(model.Subject) ? "(Konu yok)" : model.Subject.Trim();
                draft.MessageDetail = cleanBody;
                draft.CategoryId = categoryId;
                draft.SendDate = DateTime.Now;
                if (draft.MessageId == 0) _context.Messages.Add(draft);
                await _context.SaveChangesAsync();

                TempData["Toast"] = "Taslak kaydedildi.";
                return RedirectToAction(nameof(Drafts));
            }

    
            if (string.IsNullOrWhiteSpace(HtmlSanitizer.ToPlainText(cleanBody)))
                ModelState.AddModelError(nameof(model.MessageDetail), "Mesaj içeriği boş olamaz.");
            if (model.CategoryId.HasValue && !await _context.Categories.AnyAsync(c => c.CategoryId == model.CategoryId))
                ModelState.AddModelError(nameof(model.CategoryId), "Geçersiz kategori.");

            if (!ModelState.IsValid)
            {
                await FillCategoriesAsync(model);
                ViewData["ActiveMenu"] = "compose";
                return View(model);
            }

            var receiverEmail = model.ReceiverEmail.Trim();
            var receiver = await _userManager.FindByEmailAsync(receiverEmail);

            // Gönderen bilgisi formdan DEĞİL, Identity hesabından alınır
            var message = draft ?? new Message();
            message.SenderEmail = email;
            message.ReceiverEmail = receiver?.Email ?? receiverEmail;   // kayıtlı kullanıcıysa e-postanın kayıtlı yazımı kullanılır
            message.Subject = model.Subject.Trim();
            message.MessageDetail = cleanBody;
            message.CategoryId = model.CategoryId!.Value;
            message.SendDate = DateTime.Now;
            message.IsDraft = false;
            message.IsRead = false;
            message.IsFavorite = false;
            message.SenderIsFavorite = false;
            if (message.MessageId == 0) _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Alıcı Mailora'da kayıtlı değilse (veya kullanıcı istediyse) gerçek e-posta olarak da gönderilir (SMTP)
            if (receiver == null || model.SendEmailCopy)
            {
                try
                {
                    var receiverName = receiver != null ? MailoraUi.FullName(receiver.Name, receiver.Surname, receiver.UserName) : receiverEmail;
                    await _mailService.SendAsync(receiverEmail, receiverName, message.Subject,
                        EmailTemplates.Message(senderName, email, message.Subject, cleanBody), replyToEmail: email, replyToName: senderName);
                    TempData["Toast"] = receiver == null
                        ? $"Mesaj {receiverEmail} adresine e-posta olarak gönderildi."
                        : "Mesaj gönderildi ve e-posta kopyası iletildi.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SMTP gönderimi başarısız: {Receiver}", receiverEmail);
                    TempData["ToastWarning"] = "Mesaj kaydedildi ancak e-posta gönderilemedi. Mail ayarlarını kontrol edin.";
                }
            }
            else
            {
                TempData["Toast"] = $"Mesaj {MailoraUi.FullName(receiver.Name, receiver.Surname, receiver.Email)} kişisinin gelen kutusuna iletildi.";
            }

            return RedirectToAction(nameof(Sendbox));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStar(int id, string side, string? returnUrl)
        {
            var user = await CurrentUserAsync();
            var m = await _context.Messages.VisibleTo(user.Email!).FirstOrDefaultAsync(x => x.MessageId == id && !x.IsDraft);
            if (m == null) return NotFoundOrForbid(id);

            bool starred;
            if (side == "in" && m.ReceiverEmail == user.Email) starred = m.IsFavorite = !m.IsFavorite;
            else if (m.SenderEmail == user.Email) starred = m.SenderIsFavorite = !m.SenderIsFavorite;
            else return Forbid();

            await _context.SaveChangesAsync();
            if (IsAjax()) return Json(new { ok = true, starred });
            TempData["Toast"] = starred ? "Mesaj yıldızlandı." : "Yıldız kaldırıldı.";
            return BackTo(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Bulk(string op, List<string> ids, string? returnUrl)
        {
            var user = await CurrentUserAsync();
            var email = user.Email!;

            var targets = ids.Select(ParseTarget).Where(t => t.Id > 0).ToList();
            if (targets.Count == 0)
            {
                if (IsAjax()) return Json(new { ok = false });
                return BackTo(returnUrl);
            }

            var idList = targets.Select(t => t.Id).Distinct().ToList();
            // Sadece bu kullanıcının görebildiği mesajlar yüklenir; başkasına ait id'ler sessizce elenir
            var messages = await _context.Messages.VisibleTo(email).Where(m => idList.Contains(m.MessageId)).ToListAsync();
            int affected = 0;

            foreach (var (id, side) in targets)
            {
                var m = messages.FirstOrDefault(x => x.MessageId == id);
                if (m == null) continue;
                bool inSide = side == "in" && m.ReceiverEmail == email && !m.IsDraft;
                bool outSide = side == "out" && m.SenderEmail == email;
                if (!inSide && !outSide) continue;

                switch (op)
                {
                    case "read" when inSide: m.IsRead = true; affected++; break;
                    case "unread" when inSide: m.IsRead = false; affected++; break;
                    case "star": if (inSide) m.IsFavorite = true; else m.SenderIsFavorite = true; affected++; break;
                    case "unstar": if (inSide) m.IsFavorite = false; else m.SenderIsFavorite = false; affected++; break;
                    case "trash":
                        if (m.IsDraft) { _context.Messages.Remove(m); }        // taslak çöpe gitmez, doğrudan silinir
                        else if (inSide) m.ReceiverIsDeleted = true; else m.SenderIsDeleted = true;
                        affected++; break;
                    case "restore":
                        if (inSide) m.ReceiverIsDeleted = false; else m.SenderIsDeleted = false;
                        affected++; break;
                    case "delete":
                        if (m.IsDraft) { _context.Messages.Remove(m); affected++; break; }
                        if (inSide) m.ReceiverIsPermanentlyDeleted = true; else m.SenderIsPermanentlyDeleted = true;
                        affected++; break;
                }
            }

            foreach (var m in messages.Where(x => x.ReceiverIsPermanentlyDeleted && x.SenderIsPermanentlyDeleted))
                _context.Messages.Remove(m);

            await _context.SaveChangesAsync();

            var text = op switch
            {
                "read" => $"{affected} mesaj okundu olarak işaretlendi.",
                "unread" => $"{affected} mesaj okunmadı olarak işaretlendi.",
                "star" => $"{affected} mesaj yıldızlandı.",
                "unstar" => $"{affected} mesajın yıldızı kaldırıldı.",
                "trash" => $"{affected} mesaj çöp kutusuna taşındı.",
                "restore" => $"{affected} mesaj geri alındı.",
                "delete" => $"{affected} mesaj kalıcı olarak silindi.",
                _ => "İşlem tamamlandı."
            };

            if (IsAjax()) return Json(new { ok = true, affected, message = text });
            TempData["Toast"] = text;
            return BackTo(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmptyTrash()
        {
            var user = await CurrentUserAsync();
            var email = user.Email!;
            var items = await _context.Messages.TrashOf(email).ToListAsync();
            foreach (var m in items)
            {
                if (m.ReceiverEmail == email && m.ReceiverIsDeleted) m.ReceiverIsPermanentlyDeleted = true;
                if (m.SenderEmail == email && m.SenderIsDeleted) m.SenderIsPermanentlyDeleted = true;
                if (m.ReceiverIsPermanentlyDeleted && m.SenderIsPermanentlyDeleted) _context.Messages.Remove(m);
            }
            await _context.SaveChangesAsync();
            TempData["Toast"] = $"Çöp kutusu boşaltıldı ({items.Count} mesaj).";
            return RedirectToAction(nameof(Trash));
        }


        private bool IsAjax() => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        private IActionResult BackTo(string? returnUrl) =>
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Inbox));

        private IActionResult NotFoundOrForbid(int id) =>
            _context.Messages.Any(m => m.MessageId == id) ? Forbid() : NotFound();

        private static (int Id, string Side) ParseTarget(string value)
        {
            var parts = (value ?? string.Empty).Split(':');
            return int.TryParse(parts[0], out var id) ? (id, parts.Length > 1 && parts[1] == "out" ? "out" : "in") : (0, "in");
        }

        private record PersonInfo(string Name, string? ImageUrl);

        // E-posta adreslerinden kayıtlı kullanıcıların ad-soyad ve fotoğraflarını bulur
        private async Task<Dictionary<string, PersonInfo>> LookupPeopleAsync(IEnumerable<string> emails)
        {
            var list = emails.Where(e => !string.IsNullOrEmpty(e)).Select(e => e.ToUpperInvariant()).Distinct().ToList();
            var users = await _userManager.Users.AsNoTracking()
                .Where(u => u.NormalizedEmail != null && list.Contains(u.NormalizedEmail))
                .Select(u => new { u.Email, u.Name, u.Surname, u.UserName, u.ImageUrl })
                .ToListAsync();
            return users.Where(u => u.Email != null)
                .GroupBy(u => u.Email!.ToLowerInvariant())
                .ToDictionary(g => g.Key, g => new PersonInfo(MailoraUi.FullName(g.First().Name, g.First().Surname, g.First().UserName), g.First().ImageUrl));
        }

        private async Task<List<MessageListItem>> ToListItemsAsync(List<Message> messages, string box, string email)
        {
            var people = await LookupPeopleAsync(messages.SelectMany(m => new[] { m.SenderEmail, m.ReceiverEmail }));
            return messages.Select(m =>
            {
              
                bool incoming = box switch
                {
                    "inbox" => true,
                    "sent" or "drafts" => false,
                    "starred" => m.ReceiverEmail == email && m.IsFavorite,
                    "trash" => m.ReceiverEmail == email && m.ReceiverIsDeleted,
                    _ => m.ReceiverEmail == email
                };
                var personEmail = incoming ? m.SenderEmail : m.ReceiverEmail;
                var person = people.GetValueOrDefault((personEmail ?? "").ToLowerInvariant());
                return new MessageListItem
                {
                    Id = m.MessageId,
                    IsIncoming = incoming,
                    PersonEmail = personEmail ?? string.Empty,
                    PersonName = person?.Name ?? (string.IsNullOrEmpty(personEmail) ? "(Alıcı yok)" : personEmail),
                    Subject = m.Subject,
                    Snippet = HtmlSanitizer.Snippet(m.MessageDetail),
                    Date = m.SendDate,
                    IsRead = !incoming || m.IsRead,
                    IsStarred = incoming ? m.IsFavorite : m.SenderIsFavorite,
                    IsDraft = m.IsDraft,
                    CategoryId = m.CategoryId,
                    CategoryName = m.Category?.CategoryName ?? "Genel",
                    CategoryColor = m.Category?.Color
                };
            }).ToList();
        }

        private async Task FillCategoriesAsync(ComposeMessageViewModel vm)
        {
            vm.Categories = await _context.Categories.AsNoTracking().OrderBy(c => c.CategoryName)
                .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.CategoryName })
                .ToListAsync();
        }

        private async Task<int> ValidCategoryIdOrDefaultAsync(int? categoryId)
        {
            if (categoryId.HasValue && await _context.Categories.AnyAsync(c => c.CategoryId == categoryId)) return categoryId.Value;
            var genel = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryName == "Genel")
                        ?? await _context.Categories.OrderBy(c => c.CategoryId).FirstOrDefaultAsync();
            if (genel != null) return genel.CategoryId;
            var created = new Category { CategoryName = "Genel", Color = "#6FA8E8" };
            _context.Categories.Add(created);
            await _context.SaveChangesAsync();
            return created.CategoryId;
        }

        private static bool LooksLikeHtml(string? s) => !string.IsNullOrEmpty(s) && System.Text.RegularExpressions.Regex.IsMatch(s, "<(p|br|div|b|strong|i|em|u|ul|ol|li|h[1-6]|blockquote|a|span)\\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

       
        private static string TextToHtml(string? text) =>
            string.Join("", (text ?? string.Empty).Replace("\r", "").Split("\n\n")
                .Select(p => $"<p>{WebUtility.HtmlEncode(p).Replace("\n", "<br>")}</p>"));
    }
}
