using PayrollBackendProject.Application.Interfaces.Services;
using PayrollBackendProject.Application.Interfaces.Utilities;

namespace PayrollBackendProject.Infrastructure.Utilities
{
    public class FileHandler : IFileHandler
    {
        public async Task<string> WriteFile(Stream fileStream, string filename, string uploadRoot, Guid batchId)
        {   
            // Store the file so it can be processed later
            string filepath = CreateFilepath(batchId, uploadRoot);

            using (var writeStream = new FileStream(filepath, FileMode.Create))
            {
                await fileStream.CopyToAsync(writeStream);
            }

            return filepath;
        }

        private string CreateFilepath(Guid batchId, string uploadRoot)
        {
            // Create a path for the uploads in a folder called uploads
            // TODO LEGITIMIZE THIS WHEN YOU DEPLOY BUT FINE FOR LOCAL DEV
            var uploadPath = "uploads";
            var uploadsFolder = Path.Combine(uploadRoot, uploadPath);

            // If the directory does not exist then create the directory
            if(!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fullFilepath = Path.Combine(uploadsFolder, $"{batchId}");
            return fullFilepath;
        }
    }
}