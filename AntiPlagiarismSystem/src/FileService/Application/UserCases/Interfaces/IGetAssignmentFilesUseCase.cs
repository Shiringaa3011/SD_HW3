using FileService.Application.DTOs;

namespace FileService.Application.UserCases.Interfaces
{
    public interface IGetAssignmentFilesUseCase
    {
        List<AssignmentFileInfo> Execute(Guid assignmentId);
    }
}
