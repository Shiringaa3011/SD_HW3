namespace FileService.Domain.ValueObjects
{
    public record Uploader
    {
        public string Name { get; }
        public string Surname { get; }
        public int GroupNumber { get; }

        public Uploader(string name, string surname, int groupNumber)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (string.IsNullOrWhiteSpace(surname))
            {
                throw new ArgumentNullException(nameof(surname));
            }
            ArgumentOutOfRangeException.ThrowIfNegative(groupNumber);
            Name = name;
            Surname = surname;
            GroupNumber = groupNumber;
        }
    }
}
