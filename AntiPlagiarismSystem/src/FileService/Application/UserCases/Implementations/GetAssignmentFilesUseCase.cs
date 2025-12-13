using FileService.Application.DTOs;
using FileService.Application.Ports;
using FileService.Application.UserCases.Interfaces;
using FileService.Domain.Entities;
using FileService.Domain.Exceptions;
using FileService.Domain.ValueObjects;

namespace FileService.Application.UserCases.Implementations
{
    public class GetAssignmentFilesUseCase(IFileMetadataRepository metadataRepository) : IGetAssignmentFilesUseCase
    {
        private readonly IFileMetadataRepository _metadataRepository = metadataRepository ?? throw new ArgumentNullException(nameof(metadataRepository));

        public List<AssignmentFileInfo> Execute(Guid assignmentId)
        {
            if (assignmentId == Guid.Empty)
            {
                throw new FileUploadException("Validation", "INVALID_ASSIGNMENT_ID", "Assignment ID is required");
            }

            AssignmentId assignment = new(assignmentId);
            List<FileUpload> files = _metadataRepository.FindByAssignmentId(assignment);

            return [.. files.Select(file => new AssignmentFileInfo
            {
                FileId = file.Id.Id,
                FileName = file.Name.Value,
                StudentName = file.Uploader.Name,
                StudentSurname = file.Uploader.Surname,
                GroupNumber = file.Uploader.GroupNumber,
                UploadDate = file.Date.Date
            })];
        }
    }
}
