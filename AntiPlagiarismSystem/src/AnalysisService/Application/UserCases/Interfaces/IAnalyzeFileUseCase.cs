using AnalysisService.Application.DTOs;

namespace AnalysisService.Application.UserCases.Interfaces
{
    public interface IAnalyzeFileUseCase
    {
        AnalyzeFileResponse Execute(AnalyzeFileRequest request);
    }
}

