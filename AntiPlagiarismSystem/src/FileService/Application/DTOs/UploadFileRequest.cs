namespace FileService.Application.DTOs
{
    public record UploadFileRequest
    {
        public required string FileName { get; init; }
        public required byte[] FileData { get; init; }
        public required string StudentName { get; init; }
        public required string StudentSurname { get; init; }
        public int GroupNumber { get; init; }
        public Guid AssignmentId { get; init; }
    }
}
