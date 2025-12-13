namespace FileService.Domain.ValueObjects
{
    public record UploadDate
    {
        public DateTime Date { get; }

        public UploadDate(DateTime date)
        {
            if (date >  DateTime.Now)
            {
                throw new ArgumentException("Incorrect date", date.ToString());
            }
            Date = date;
        }
    }
}
