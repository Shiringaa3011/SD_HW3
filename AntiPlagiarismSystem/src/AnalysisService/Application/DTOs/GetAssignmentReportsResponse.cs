namespace AnalysisService.Application.DTOs
{
    public record AssignmentReportInfo
    {
        public Guid ReportId { get; init; }
        public Guid FileId { get; init; }
        public string Status { get; init; } = string.Empty;
        public bool IsPlagiarized { get; init; }
        public double SimilarityPercentage { get; init; }
        public DateTime AnalysisDate { get; init; }
    }

    public record GetAssignmentReportsResponse
    {
        public Guid AssignmentId { get; init; }
        public List<AssignmentReportInfo> Reports { get; init; } = [];
    }
}

