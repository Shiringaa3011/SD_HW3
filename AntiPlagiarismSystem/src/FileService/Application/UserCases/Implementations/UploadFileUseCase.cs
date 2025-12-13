using FileService.Application.DTOs;
using FileService.Application.Ports;
using FileService.Application.UserCases.Interfaces;
using FileService.Domain.Entities;
using FileService.Domain.Exceptions;
using FileService.Domain.ValueObjects;

namespace FileService.Application.UserCases.Implementations
{
    public class UploadFileUseCase(
        IFileStorage fileStorage,
        IFileMetadataRepository metadataRepository) : IUploadFileUseCase
    {
        private readonly IFileStorage _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        private readonly IFileMetadataRepository _metadataRepository = metadataRepository ?? throw new ArgumentNullException(nameof(metadataRepository));

        public UploadFileResponse Execute(UploadFileRequest request)
        {
            ValidateRequest(request);

            string fileIdentifier = _fileStorage.SaveFile(request.FileData, request.FileName);

            try
            {
                FileUpload fileUpload = FileUpload.Create(
                    new FileName(fileIdentifier),
                    new FileSize(request.FileData.Length),
                    new UploadDate(DateTime.UtcNow),
                    new Uploader(
                        request.StudentName,
                        request.StudentSurname ?? string.Empty,
                        request.GroupNumber
                    ),
                    new AssignmentId(request.AssignmentId)
                );

                _metadataRepository.Save(fileUpload);

                return new UploadFileResponse
                {
                    FileId = fileUpload.Id.Id,
                    FileIdentifier = fileIdentifier,
                    UploadDate = DateTime.UtcNow,
                    IsPlagiarized = false
                };
            }
            catch
            {
                _ = _fileStorage.TryDelete(fileIdentifier);
                throw;
            }
        }

        private void ValidateRequest(UploadFileRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.FileName))
            {
                throw new FileUploadException("Validation", "INVALID_FILENAME", "File name is required");
            }

            if (request.FileData == null || request.FileData.Length == 0)
            {
                throw new FileUploadException("Validation", "EMPTY_FILE", "File data is required");
            }

            if (string.IsNullOrWhiteSpace(request.StudentName))
            {
                throw new FileUploadException("Validation", "INVALID_STUDENT", "Student name is required");
            }

            if (request.AssignmentId == Guid.Empty)
            {
                throw new FileUploadException("Validation", "INVALID_ASSIGNMENT", "Assignment ID is required");
            }
        }
    }
}
