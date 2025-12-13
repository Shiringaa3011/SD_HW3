namespace AnalysisService.Application.DTOs
{
    public record AnalyzeFileRequest
    {
        public required Guid FileId { get; init; }
    }
}

