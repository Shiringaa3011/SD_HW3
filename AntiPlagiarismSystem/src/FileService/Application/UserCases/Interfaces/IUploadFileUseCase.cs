using FileService.Application.DTOs;

namespace FileService.Application.UserCases.Interfaces
{
    public interface IUploadFileUseCase
    {
        UploadFileResponse Execute(UploadFileRequest request);
    }
}
