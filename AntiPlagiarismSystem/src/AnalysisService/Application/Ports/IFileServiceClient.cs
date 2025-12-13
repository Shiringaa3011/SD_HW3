namespace AnalysisService.Application.Ports
{
    public interface IFileServiceClient
    {
        FileMetadata? GetFileMetadata(Guid fileId);
        byte[] GetFileData(Guid fileId);
        List<AssignmentFileInfo> GetAssignmentFiles(Guid assignmentId);
    }

    public record FileMetadata
    {
        public Guid FileId { get; init; }
        public string FileName { get; init; } = string.Empty;
        public long FileSize { get; init; }
        public DateTime UploadDate { get; init; }
        public string StudentName { get; init; } = string.Empty;
        public string StudentSurname { get; init; } = string.Empty;
        public int GroupNumber { get; init; }
        public Guid AssignmentId { get; init; }
    }

    public record AssignmentFileInfo
    {
        public Guid FileId { get; init; }
        public string FileName { get; init; } = string.Empty;
        public DateTime UploadDate { get; init; }
        public string StudentName { get; init; } = string.Empty;
        public string StudentSurname { get; init; } = string.Empty;
        public int GroupNumber { get; init; }
    }
}

