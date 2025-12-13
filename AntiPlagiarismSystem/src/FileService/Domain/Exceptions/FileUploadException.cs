using FileService.Domain.ValueObjects;
using System;

namespace FileService.Domain.Exceptions
{
    public class FileUploadException : Exception
    {
        public FileUploadId? FileId { get; }
        public string ErrorType { get; }
        public string ErrorCode { get; }

        public FileUploadException(string message)
            : base(message)
        {
            ErrorType = "Internal";
            ErrorCode = "INTERNAL_ERROR";
        }

        public FileUploadException(string errorType, string errorCode, string message)
            : base(message)
        {
            ErrorType = errorType;
            ErrorCode = errorCode;
        }

        public FileUploadException(string errorType, string errorCode, string message, FileUploadId fileId)
            : base(message)
        {
            ErrorType = errorType;
            ErrorCode = errorCode;
            FileId = fileId;
        }

        public FileUploadException(string errorType, string errorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorType = errorType;
            ErrorCode = errorCode;
        }

        public FileUploadException(string errorType, string errorCode, string message, FileUploadId fileId, Exception innerException)
            : base(message, innerException)
        {
            ErrorType = errorType;
            ErrorCode = errorCode;
            FileId = fileId;
        }
    }
}