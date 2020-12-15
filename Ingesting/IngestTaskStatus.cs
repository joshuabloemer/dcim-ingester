namespace DcimIngester.Ingesting
{
    public enum IngestTaskStatus
    {
        Prompting,
        Ingesting,
        Completed,
        Failed,
        Cancelled
    }
}
