using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MYAcademy_IdentityEmailProject.Entities;

namespace MYAcademy_IdentityEmailProject.Context
{
    public class EmailContext : IdentityDbContext<AppUser>
    {
        // Bağlantı cümlesi artık appsettings.json → "ConnectionStrings:DefaultConnection" içinde.
        // Program.cs'teki AddDbContext(...) bu ayarı okuyup context'e verir.
        public EmailContext(DbContextOptions<EmailContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);   // Identity tablolarının ayarları için ZORUNLU

            builder.Entity<Message>(e =>
            {
                e.Property(x => x.Subject).HasMaxLength(200);
                e.Property(x => x.SenderEmail).HasMaxLength(256);
                e.Property(x => x.ReceiverEmail).HasMaxLength(256);

                // Gelen kutusu / gönderilenler sorguları bu alanlarla filtrelendiği için index
                e.HasIndex(x => x.ReceiverEmail);
                e.HasIndex(x => x.SenderEmail);
            });

            builder.Entity<Category>(e =>
            {
                e.Property(x => x.CategoryName).HasMaxLength(50);
                e.Property(x => x.Color).HasMaxLength(20);
            });
        }
    }
}
