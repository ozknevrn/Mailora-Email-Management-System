using Microsoft.EntityFrameworkCore;
using MYAcademy_IdentityEmailProject.Context;
using MYAcademy_IdentityEmailProject.Entities;

namespace MYAcademy_IdentityEmailProject.Data
{

    public static class DbSeeder
    {
        private static readonly (string Name, string Color)[] DefaultCategories =
        {
            ("İş", "#8B7FD6"),
            ("Kişisel", "#5CC09A"),
            ("Fatura", "#EBA27A"),
            ("Genel", "#6FA8E8")
        };

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EmailContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<EmailContext>>();

            try
            {
                var existing = await context.Categories.ToListAsync();
                foreach (var (name, color) in DefaultCategories)
                {
                    var cat = existing.FirstOrDefault(c => c.CategoryName == name);
                    if (cat == null)
                        context.Categories.Add(new Category { CategoryName = name, Color = color });
                    else if (string.IsNullOrEmpty(cat.Color))
                        cat.Color = color;
                }
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                
                logger.LogWarning(ex, "Kategori seed işlemi atlandı. 'update-database' komutunu çalıştırdığınızdan emin olun.");
            }
        }
    }
}
