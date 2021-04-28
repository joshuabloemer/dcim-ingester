namespace DcimIngester.Ingesting
{
    /// <summary>
    /// Specifies the status of an <see cref="IngestTask"/>.
    /// </summary>
    public enum IngestTaskStatus
    {
        Ready,
        Ingesting,
        Completed,
        Failed,
        Aborted
    }
}
