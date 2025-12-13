namespace AnalysisService.Domain.ValueObjects
{
    public record AnalysisDate
    {
        public DateTime Date { get; }

        public AnalysisDate(DateTime date)
        {
            Date = date;
        }

        public static AnalysisDate Now()
        {
            return new(DateTime.UtcNow);
        }
    }
}

