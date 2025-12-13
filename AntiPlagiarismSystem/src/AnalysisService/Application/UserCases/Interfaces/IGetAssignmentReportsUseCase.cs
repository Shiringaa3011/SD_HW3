using AnalysisService.Application.DTOs;

namespace AnalysisService.Application.UserCases.Interfaces
{
    public interface IGetAssignmentReportsUseCase
    {
        GetAssignmentReportsResponse Execute(GetAssignmentReportsRequest request);
    }
}

