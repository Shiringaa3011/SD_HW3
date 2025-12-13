namespace AnalysisService.Application.DTOs
{
    public record GetAssignmentReportsRequest
    {
        public required Guid AssignmentId { get; init; }
    }
}

