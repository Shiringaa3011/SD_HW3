namespace FileService.Application.DTOs
{
    public record FileMetadata
    {
        public Guid FileId { get; init; }
        public required string FileName { get; init; }
        public long FileSize { get; init; }
        public required string StudentName { get; init; }
        public required string StudentSurname { get; init; }
        public int GroupNumber { get; init; }
        public DateTime UploadDate { get; init; }
        public Guid AssignmentId { get; init; }
    }
}
