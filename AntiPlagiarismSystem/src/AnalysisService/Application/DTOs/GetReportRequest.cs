namespace AnalysisService.Application.DTOs
{
    public record GetReportRequest
    {
        public required Guid ReportId { get; init; }
    }
}

