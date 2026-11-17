namespace MYAcademy_IdentityEmailProject.Entities
{
   
    public class Message
    {
        public int MessageId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string MessageDetail { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string ReceiverEmail { get; set; } = string.Empty;
        public DateTime SendDate { get; set; }

        
        public bool IsRead { get; set; }
        public bool IsFavorite { get; set; }               
        public bool ReceiverIsDeleted { get; set; }        
        public bool ReceiverIsPermanentlyDeleted { get; set; }

       
        public bool SenderIsFavorite { get; set; }         
        public bool SenderIsDeleted { get; set; }          
        public bool SenderIsPermanentlyDeleted { get; set; }

        
        public bool IsDraft { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}
