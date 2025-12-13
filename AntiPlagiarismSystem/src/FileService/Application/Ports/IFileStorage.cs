namespace FileService.Application.Ports
{
    // Интерфейс файлового хранилища
    public interface IFileStorage
    {
        string SaveFile(byte[] fileData, string fileName);

        byte[] GetFile(string fileIdentifier);

        string GetFileUrl(string fileIdentifier);

        bool TryDelete(string fileIdentifier);
    }
}
