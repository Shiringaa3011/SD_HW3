namespace AnalysisService.Domain.ValueObjects
{
    public record SimilarityPercentage
    {
        public double Value { get; }

        public SimilarityPercentage(double value)
        {
            if (value is < 0 or > 100)
            {
                throw new ArgumentException("Similarity percentage must be between 0 and 100", nameof(value));
            }
            Value = value;
        }
    }
}

