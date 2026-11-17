namespace MYAcademy_IdentityEmailProject.Services
{
    public interface IImageUploadService
    {
       
        Task<string> SaveAsync(IFormFile file, string folder);

        void Delete(string? relativePath);

       
        string? Validate(IFormFile? file);
    }
}
