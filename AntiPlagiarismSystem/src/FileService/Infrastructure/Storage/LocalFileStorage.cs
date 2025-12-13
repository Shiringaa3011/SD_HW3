using FileService.Application.Ports;

namespace FileService.Infrastructure.Storage
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _storagePath;

        public LocalFileStorage(string storagePath)
        {
            _storagePath = storagePath;

            // Создаем директорию, если не существует
            if (!Directory.Exists(_storagePath))
            {
                _ = Directory.CreateDirectory(_storagePath);
            }
        }

        public string SaveFile(byte[] fileData, string fileName)
        {
            if (!Directory.Exists(_storagePath))
            {
                throw new DirectoryNotFoundException("Directory not found");
            }

            // Генерируем уникальное имя файла
            string uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            string filePath = Path.Combine(_storagePath, uniqueFileName);

            File.WriteAllBytes(filePath, fileData);

            return uniqueFileName;
        }

        public byte[] GetFile(string fileIdentifier)
        {
            string filePath = Path.Combine(_storagePath, fileIdentifier);

            return !File.Exists(filePath) ? throw new FileNotFoundException($"File not found: {fileIdentifier}") : File.ReadAllBytes(filePath);
        }

        public string GetFileUrl(string fileIdentifier)
        {
            // Для локального хранилища возвращаем путь к файлу
            return Path.Combine(_storagePath, fileIdentifier);
        }

        public bool TryDelete(string fileIdentifier)
        {
            string filePath = Path.Combine(_storagePath, fileIdentifier);

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
