namespace FileService.Application.DTOs
{
    public record GetFileRequest
    {
        public Guid FileId { get; init; }
        public bool IncludeMetadata { get; init; } = true;
    }
}
