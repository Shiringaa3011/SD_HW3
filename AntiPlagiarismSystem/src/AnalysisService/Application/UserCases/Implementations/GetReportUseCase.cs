using AnalysisService.Application.DTOs;
using AnalysisService.Application.Ports;
using AnalysisService.Application.UserCases.Interfaces;
using AnalysisService.Domain.Entities;
using AnalysisService.Domain.Exceptions;
using AnalysisService.Domain.ValueObjects;

namespace AnalysisService.Application.UserCases.Implementations
{
    public class GetReportUseCase(
        IAnalysisReportRepository reportRepository) : IGetReportUseCase
    {
        private readonly IAnalysisReportRepository _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));

        public GetReportResponse? Execute(GetReportRequest request)
        {
            if (request.ReportId == Guid.Empty)
            {
                throw new AnalysisException("Validation", "INVALID_REPORT_ID", "Report ID cannot be empty");
            }

            AnalysisReportId reportId;
            try
            {
                reportId = new AnalysisReportId(request.ReportId);
            }
            catch (ArgumentException ex)
            {
                throw new AnalysisException("Validation", "INVALID_REPORT_ID", ex.Message, ex);
            }

            AnalysisReport? report = _reportRepository.FindById(reportId);

            return report == null
                ? null
                : new GetReportResponse
            {
                ReportId = report.Id.Id,
                FileId = report.FileId.Id,
                IsPlagiarized = report.IsPlagiarized,
                SimilarityPercentage = report.SimilarityPercentage.Value,
                AnalysisDate = report.AnalysisDate.Date,
                ReportFilePath = report.ReportFilePath
            };
        }
    }
}

