using FileService.Application.DTOs;
using FileService.Application.Ports;
using FileService.Application.UserCases.Interfaces;
using FileService.Domain.Entities;
using FileService.Domain.Exceptions;
using FileService.Domain.ValueObjects;

namespace FileService.Application.UserCases.Implementations
{
    public class GetFileUseCase(
        IFileStorage fileStorage,
        IFileMetadataRepository metadataRepository) : IGetFileUseCase
    {
        private readonly IFileStorage _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        private readonly IFileMetadataRepository _metadataRepository = metadataRepository ?? throw new ArgumentNullException(nameof(metadataRepository));

        public GetFileResponse Execute(GetFileRequest request)
        {
            if (request.FileId == Guid.Empty)
            {
                throw new FileUploadException(
                    errorType: "Validation",
                    errorCode: "INVALID_FILE_ID",
                    message: "File ID is required"
                );
            }

            FileUploadId fileId = new(request.FileId);
            FileUpload fileUpload = _metadataRepository.FindById(fileId) ?? throw new FileUploadException(
                    errorType: "NotFound",
                    errorCode: "FILE_NOT_FOUND",
                    message: $"File with ID {request.FileId} not found",
                    fileId: fileId
                );

            GetFileResponse response;

            try
            {
                byte[] fileData = _fileStorage.GetFile(fileUpload.Name.Value);

                response = new()
                {
                    FileData = fileData,
                    FileName = fileUpload.Name.Value,
                };
            } catch (System.IO.FileNotFoundException ex)
            {
                throw new FileUploadException(
                    errorType: "Internal",
                    errorCode: "FILE_STORAGE_CORRUPTED",
                    message: "File data corrupted - exists in DB but not in storage",
                    fileId: fileId,
                    innerException: ex
                );
            }

            // Добавляем метаданные если нужно
            if (request.IncludeMetadata)
            {
                response = response with
                {
                    Metadata = new FileMetadata
                    {
                        FileName = fileUpload.Name.Value,
                        FileId = fileUpload.Id.Id,
                        StudentName = fileUpload.Uploader.Name,
                        StudentSurname = fileUpload.Uploader.Surname,
                        GroupNumber = fileUpload.Uploader.GroupNumber,
                        UploadDate = fileUpload.Date.Date,
                        AssignmentId = fileUpload.AssignmentId.Id
                    }
                };
            }

            return response;
        }
    }
}
