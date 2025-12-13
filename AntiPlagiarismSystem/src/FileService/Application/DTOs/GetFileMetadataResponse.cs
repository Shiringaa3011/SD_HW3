namespace FileService.Application.DTOs
{
    public record GetFileMetadataResponse
    {
        public FileMetadata? Metadata { get; init; }
        public string? FileUrl { get; init; }
        public bool FileExists { get; init; }
    }
}
