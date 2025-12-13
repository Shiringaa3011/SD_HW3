namespace FileService.Application.DTOs
{
    public record GetFileMetadataRequest
    {
        public Guid FileId { get; init; }
    }
}
