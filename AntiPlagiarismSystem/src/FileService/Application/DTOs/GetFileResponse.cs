namespace FileService.Application.DTOs
{
    public record GetFileResponse
    {
        public required byte[] FileData { get; init; }
        public required string FileName { get; init; }
        public FileMetadata? Metadata { get; init; }
    }
}
