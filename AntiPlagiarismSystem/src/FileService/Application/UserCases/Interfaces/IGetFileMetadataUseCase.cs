using FileService.Application.DTOs;

namespace FileService.Application.UserCases.Interfaces
{
    public interface IGetFileMetadataUseCase
    {
        GetFileMetadataResponse Execute(GetFileMetadataRequest request);

    }
}
