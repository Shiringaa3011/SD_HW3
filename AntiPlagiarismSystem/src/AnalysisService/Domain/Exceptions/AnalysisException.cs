namespace AnalysisService.Domain.Exceptions
{
    public class AnalysisException : Exception
    {
        public string ErrorType { get; }
        public string ErrorCode { get; }

        public AnalysisException(string message)
            : base(message)
        {
            ErrorType = "Internal";
            ErrorCode = "INTERNAL_ERROR";
        }

        public AnalysisException(string errorType, string errorCode, string message)
            : base(message)
        {
            ErrorType = errorType;
            ErrorCode = errorCode;
        }

        public AnalysisException(string errorType, string errorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorType = errorType;
            ErrorCode = errorCode;
        }
    }
}

