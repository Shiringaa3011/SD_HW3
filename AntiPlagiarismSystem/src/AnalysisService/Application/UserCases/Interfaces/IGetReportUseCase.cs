using AnalysisService.Application.DTOs;

namespace AnalysisService.Application.UserCases.Interfaces
{
    public interface IGetReportUseCase
    {
        GetReportResponse? Execute(GetReportRequest request);
    }
}

