using FileService.Application.DTOs;

namespace FileService.Application.UserCases.Interfaces
{
    public interface IGetFileUseCase
    {
        GetFileResponse Execute(GetFileRequest request);
    }
}
