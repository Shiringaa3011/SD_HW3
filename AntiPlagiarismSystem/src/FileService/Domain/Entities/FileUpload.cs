using FileService.Domain.ValueObjects;
using System;

namespace FileService.Domain.Entities
{
    public class FileUpload
    {
        public FileUploadId Id { get; }
        public FileName Name { get; }
        public FileSize Size { get; }
        public UploadDate Date { get; }
        public Uploader Uploader { get; }
        public AssignmentId AssignmentId { get; }

        private FileUpload(FileUploadId id, FileName name, FileSize size, UploadDate date, Uploader uploader, AssignmentId assignmentId)
        {
            Id = id;
            Name = name;
            Size = size;
            Date = date;
            Uploader = uploader;
            AssignmentId = assignmentId;
        }

        public static FileUpload Create(FileName name, FileSize size, UploadDate date, Uploader uploader, AssignmentId assignmentId)
        {
            return new FileUpload(FileUploadId.New(), name, size, date, uploader, assignmentId);
        }

        public static FileUpload Restore(FileUploadId id, FileName name, FileSize size, UploadDate date, Uploader uploader, AssignmentId assignmentId)
        {
            return new FileUpload(id, name, size, date, uploader, assignmentId);
        }
    }
}