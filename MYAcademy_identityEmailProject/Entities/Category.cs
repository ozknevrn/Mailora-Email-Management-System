namespace MYAcademy_IdentityEmailProject.Entities
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

      
        public string? Color { get; set; }

        public List<Message> Messages { get; set; } = new();
    }
}
