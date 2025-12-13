using FileService.Domain.Entities;
using FileService.Domain.ValueObjects;

namespace FileService.Application.Ports
{
    // Интерфейс репозитория для работы с метаданными файлов
    public interface IFileMetadataRepository
    {
        void Save(FileUpload fileUpload);

        FileUpload? FindById(FileUploadId id);

        List<FileUpload> FindByAssignmentId(AssignmentId assignmentId);
    }
}
