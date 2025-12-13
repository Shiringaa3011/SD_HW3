namespace AnalysisService.Domain.ValueObjects
{
    public record FileId
    {
        public Guid Id { get; }

        public FileId(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Incorrect ID", nameof(id));
            }
            Id = id;
        }
    }
}

