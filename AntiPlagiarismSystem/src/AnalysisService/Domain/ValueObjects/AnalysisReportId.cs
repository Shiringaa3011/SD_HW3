namespace AnalysisService.Domain.ValueObjects
{
    public record AnalysisReportId
    {
        public Guid Id { get; }

        public AnalysisReportId(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Incorrect ID", nameof(id));
            }
            Id = id;
        }

        public static AnalysisReportId New()
        {
            return new(Guid.NewGuid());
        }
    }
}

