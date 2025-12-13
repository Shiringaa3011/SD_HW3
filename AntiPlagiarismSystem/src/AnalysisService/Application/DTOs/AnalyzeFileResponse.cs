namespace AnalysisService.Application.DTOs
{
    public record AnalyzeFileResponse
    {
        public Guid ReportId { get; init; }
        public Guid FileId { get; init; }
        public bool IsPlagiarized { get; init; }
        public double SimilarityPercentage { get; init; }
        public DateTime AnalysisDate { get; init; }
    }
}

