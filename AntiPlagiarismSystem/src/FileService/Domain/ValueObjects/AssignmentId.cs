namespace FileService.Domain.ValueObjects
{
    public record AssignmentId
    {
        public Guid Id { get; }

        public AssignmentId(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Incorrect ID", nameof(id));
            }
            Id = id;
        }
    }
}
