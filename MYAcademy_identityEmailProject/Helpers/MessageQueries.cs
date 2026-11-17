using MYAcademy_IdentityEmailProject.Entities;

namespace MYAcademy_IdentityEmailProject.Helpers
{
    
     
    public static class MessageQueries
    {
        public static IQueryable<Message> InboxOf(this IQueryable<Message> q, string email) =>
            q.Where(m => m.ReceiverEmail == email && !m.IsDraft && !m.ReceiverIsDeleted && !m.ReceiverIsPermanentlyDeleted);

        public static IQueryable<Message> SentOf(this IQueryable<Message> q, string email) =>
            q.Where(m => m.SenderEmail == email && !m.IsDraft && !m.SenderIsDeleted && !m.SenderIsPermanentlyDeleted);

        public static IQueryable<Message> StarredOf(this IQueryable<Message> q, string email) =>
            q.Where(m => !m.IsDraft && (
                (m.ReceiverEmail == email && m.IsFavorite && !m.ReceiverIsDeleted && !m.ReceiverIsPermanentlyDeleted) ||
                (m.SenderEmail == email && m.SenderIsFavorite && !m.SenderIsDeleted && !m.SenderIsPermanentlyDeleted)));

        public static IQueryable<Message> DraftsOf(this IQueryable<Message> q, string email) =>
            q.Where(m => m.SenderEmail == email && m.IsDraft && !m.SenderIsPermanentlyDeleted);

        public static IQueryable<Message> TrashOf(this IQueryable<Message> q, string email) =>
            q.Where(m => !m.IsDraft && (
                (m.ReceiverEmail == email && m.ReceiverIsDeleted && !m.ReceiverIsPermanentlyDeleted) ||
                (m.SenderEmail == email && m.SenderIsDeleted && !m.SenderIsPermanentlyDeleted)));

      
        public static IQueryable<Message> VisibleTo(this IQueryable<Message> q, string email) =>
            q.Where(m =>
                (m.ReceiverEmail == email && !m.IsDraft && !m.ReceiverIsPermanentlyDeleted) ||
                (m.SenderEmail == email && !m.SenderIsPermanentlyDeleted));

        public static IQueryable<Message> ForBox(this IQueryable<Message> q, string box, string email) => box switch
        {
            "sent" => q.SentOf(email),
            "starred" => q.StarredOf(email),
            "drafts" => q.DraftsOf(email),
            "trash" => q.TrashOf(email),
            _ => q.InboxOf(email)
        };
    }
}
