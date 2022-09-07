namespace DcimIngester
{
    /// <summary>
    /// Specifies the destination subfolder structure to ingest files into.
    /// </summary>
    public enum DestStructure
    {
        /// <summary>
        /// Destination/YYYY/MM/DD directory.
        /// </summary>
        Year_Month_Day,
        /// <summary>
        /// Destination/YYYY/YYYY-MM-DD directory.
        /// </summary>
        Year_YearMonthDay,
        /// <summary>
        /// Destination/YYYY-MM-DD directory.
        /// </summary>
        YearMonthDay
    }
}
