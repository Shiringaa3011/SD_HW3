using AnalysisService.Application.DTOs;
using AnalysisService.Application.Ports;
using AnalysisService.Application.UserCases.Interfaces;
using AnalysisService.Domain.Entities;
using AnalysisService.Domain.ValueObjects;

namespace AnalysisService.Application.UserCases.Implementations
{
    public class GetAssignmentReportsUseCase(
        IAnalysisReportRepository reportRepository,
        IFileServiceClient fileServiceClient) : IGetAssignmentReportsUseCase
    {
        private readonly IAnalysisReportRepository _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
        private readonly IFileServiceClient _fileServiceClient = fileServiceClient ?? throw new ArgumentNullException(nameof(fileServiceClient));

        public GetAssignmentReportsResponse Execute(GetAssignmentReportsRequest request)
        {
            if (request.AssignmentId == Guid.Empty)
            {
                throw new Domain.Exceptions.AnalysisException("Validation", "INVALID_ASSIGNMENT_ID", "Assignment ID cannot be empty");
            }

            List<AssignmentFileInfo> files = _fileServiceClient.GetAssignmentFiles(request.AssignmentId);

            List<AssignmentReportInfo> reports = [];

            foreach (AssignmentFileInfo file in files)
            {
                AnalysisReport? report = _reportRepository.FindByFileId(new FileId(file.FileId));
                
                if (report != null)
                {
                    reports.Add(new AssignmentReportInfo
                    {
                        ReportId = report.Id.Id,
                        FileId = file.FileId,
                        Status = "completed",
                        IsPlagiarized = report.IsPlagiarized,
                        SimilarityPercentage = report.SimilarityPercentage.Value,
                        AnalysisDate = report.AnalysisDate.Date
                    });
                }
                else
                {
                    // Если отчета нет, добавляем со статусом "pending"
                    reports.Add(new AssignmentReportInfo
                    {
                        ReportId = Guid.Empty,
                        FileId = file.FileId,
                        Status = "pending",
                        IsPlagiarized = false,
                        SimilarityPercentage = 0.0,
                        AnalysisDate = file.UploadDate
                    });
                }
            }

            return new GetAssignmentReportsResponse
            {
                AssignmentId = request.AssignmentId,
                Reports = reports
            };
        }
    }
}

