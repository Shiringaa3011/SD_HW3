using System;

namespace FileService.Domain.ValueObjects
{
    public record FileUploadId
    {
        public Guid Id { get; }

        public FileUploadId(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Incorrect ID", nameof(id));
            }
            Id = id;
        }

        public static FileUploadId New()
        {
            return new(Guid.NewGuid());
        }
    }
}