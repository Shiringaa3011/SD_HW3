namespace FileService.Application.DTOs
{
    public record UploadFileResponse
    {
        public Guid FileId { get; init; }
        public required string FileIdentifier { get; init; }
        public DateTime UploadDate { get; init; }
        public bool IsPlagiarized { get; init; }
    }
}
