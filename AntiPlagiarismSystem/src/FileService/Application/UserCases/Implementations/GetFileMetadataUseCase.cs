using FileService.Application.DTOs;
using FileService.Application.Ports;
using FileService.Application.UserCases.Interfaces;
using FileService.Domain.Entities;
using FileService.Domain.Exceptions;
using FileService.Domain.ValueObjects;

namespace FileService.Application.UserCases.Implementations
{
    public class GetFileMetadataUseCase(
        IFileMetadataRepository metadataRepository,
        IFileStorage fileStorage) : IGetFileMetadataUseCase
    {
        private readonly IFileMetadataRepository _metadataRepository = metadataRepository ?? throw new ArgumentNullException(nameof(metadataRepository));
        private readonly IFileStorage _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));

        public GetFileMetadataResponse Execute(GetFileMetadataRequest request)
        {
            if (request.FileId == Guid.Empty)
            {
                throw new FileUploadException("Validation", "INVALID_FILE_ID", "File ID is required");
            }

            // Получаем метаданные
            FileUploadId fileId = new(request.FileId);
            FileUpload? fileUpload = _metadataRepository.FindById(fileId);

            if (fileUpload == null)
            {
                return new GetFileMetadataResponse
                {
                    FileExists = false,
                    Metadata = null,
                    FileUrl = null
                };
            }

            // Получаем URL файла
            string fileUrl = _fileStorage.GetFileUrl(fileUpload.Name.Value);

            return new GetFileMetadataResponse
            {
                FileExists = true,
                FileUrl = fileUrl,
                Metadata = new FileMetadata
                {
                    FileId = fileUpload.Id.Id,
                    FileName = fileUpload.Name.Value,
                    FileSize = fileUpload.Size.Bytes,
                    StudentName = fileUpload.Uploader.Name,
                    StudentSurname = fileUpload.Uploader.Surname,
                    GroupNumber = fileUpload.Uploader.GroupNumber,
                    UploadDate = fileUpload.Date.Date,
                    AssignmentId = fileUpload.AssignmentId.Id
                }
            };
        }
    }
}
