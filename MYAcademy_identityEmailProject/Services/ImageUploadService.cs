namespace MYAcademy_IdentityEmailProject.Services
{
    
    public class ImageUploadService : IImageUploadService
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const long MaxBytes = 2 * 1024 * 1024; // 2 MB

        private readonly IWebHostEnvironment _env;

        public ImageUploadService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public string? Validate(IFormFile? file)
        {
            if (file == null || file.Length == 0) return "Lütfen bir resim dosyası seçin.";
            if (file.Length > MaxBytes) return "Profil fotoğrafı en fazla 2 MB olabilir.";
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext) || !file.ContentType.StartsWith("image/"))
                return "Yalnızca JPG, PNG, WEBP veya GIF yükleyebilirsiniz.";
            return null;
        }

        public async Task<string> SaveAsync(IFormFile file, string folder)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var dir = Path.Combine(_env.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(dir);

          
            var fileName = $"{Guid.NewGuid():N}{ext}";
            await using (var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return $"/uploads/{folder}/{fileName}";
        }

        public void Delete(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || !relativePath.StartsWith("/uploads/")) return;
            var full = Path.GetFullPath(Path.Combine(_env.WebRootPath, relativePath.TrimStart('/')));
            var root = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads"));
            if (full.StartsWith(root) && File.Exists(full)) File.Delete(full);
        }
    }
}
