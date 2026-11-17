using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MYAcademy_IdentityEmailProject.Models
{
   
    public class MessageListViewModel
    {
        public string Box { get; set; } = "inbox";          
        public string Title { get; set; } = "Gelen Kutusu";
        public string Icon { get; set; } = "bi-inbox";
        public string EmptyText { get; set; } = "Yeni mesajlar geldiğinde burada listelenecek.";
        public string UserEmail { get; set; } = string.Empty;

        
        public string? Query { get; set; }
        public int? CategoryId { get; set; }
        public string Sort { get; set; } = "desc";
        public string Filter { get; set; } = "all";       

        public List<MessageListItem> Items { get; set; } = new();
        public List<CategoryOption> Categories { get; set; } = new();
        public int UnreadCount { get; set; }
        public int StarredCount { get; set; }
        public int BoxTotal { get; set; }                    

        
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }                  
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    }

    public class MessageListItem
    {
        public int Id { get; set; }
        public string PersonName { get; set; } = string.Empty;   
        public string PersonEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool IsRead { get; set; } = true;
        public bool IsStarred { get; set; }
        public bool IsDraft { get; set; }
        public bool IsIncoming { get; set; }                     
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? CategoryColor { get; set; }
    }

    public class CategoryOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; }
        public int Count { get; set; }
    }

    public class MessageDetailViewModel
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string SafeHtmlBody { get; set; } = string.Empty;   
        public DateTime SendDate { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? CategoryColor { get; set; }

        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string? SenderImageUrl { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverEmail { get; set; } = string.Empty;

        public bool IsIncoming { get; set; }          
        public bool IsStarred { get; set; }
        public bool IsInTrash { get; set; }
        public bool ReceiverIsRegistered { get; set; }

        public string BackBox { get; set; } = "inbox";
        public int? PrevId { get; set; }
        public int? NextId { get; set; }
    }

    public class ComposeMessageViewModel
    {
        public int? DraftId { get; set; }

        [Required(ErrorMessage = "Alıcı e-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        [StringLength(256)]
        public string ReceiverEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lütfen bir kategori seçin.")]
        public int? CategoryId { get; set; }

        [Required(ErrorMessage = "Konu alanı boş bırakılamaz.")]
        [StringLength(200, ErrorMessage = "Konu en fazla 200 karakter olabilir.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mesaj içeriği boş olamaz.")]
        public string MessageDetail { get; set; } = string.Empty;

        
        public bool SendEmailCopy { get; set; }

        
        public string Mode { get; set; } = "new";

        public List<SelectListItem> Categories { get; set; } = new();
    }

    public class DashboardViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public int InboxCount { get; set; }
        public int UnreadCount { get; set; }
        public int SentCount { get; set; }
        public int StarredCount { get; set; }
        public int DraftCount { get; set; }
        public int TrashCount { get; set; }
        public int TodayReceived { get; set; }
        public int WeekSent { get; set; }

        public List<MessageListItem> RecentMessages { get; set; } = new();
        public List<CategoryOption> CategoryDistribution { get; set; } = new();
        public List<ContactStat> TopContacts { get; set; } = new();

       
        public List<string> TrafficLabels { get; set; } = new();
        public List<int> TrafficIncoming { get; set; } = new();
        public List<int> TrafficSent { get; set; } = new();
    }

    public class ContactStat
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Total { get; set; }
    }
}
