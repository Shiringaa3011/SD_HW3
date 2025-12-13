using System;

namespace FileService.Domain.ValueObjects
{
    public record FileSize
    {
        public long Bytes { get; }
        private const long MaxSize = 10 * 1024 * 1024;

        public FileSize(long bytes)
        {
            if (bytes <= 0)
            {
                throw new ArgumentException($"Invalid file size: {bytes}. Size cannot be negative", nameof(bytes));
            }
            if (bytes > MaxSize)
            {
                throw new ArgumentException($"File too large: {bytes} bytes. Maximum is {MaxSize} bytes", nameof(bytes));
            }
            Bytes = bytes;
        }
    }
}