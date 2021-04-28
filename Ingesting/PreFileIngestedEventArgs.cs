using System;

namespace DcimIngester.Ingesting
{
    /// <summary>
    /// Represents event data about a file that is about to be ingested.
    /// </summary>
    public class PreFileIngestedEventArgs : EventArgs
    {
        /// <summary>
        /// The index of the file in the list of files to ingest.
        /// </summary>
        public int FileNumber { get; private set; }

        /// <summary>
        /// Initialises a new instance of the <see cref="PreFileIngestedEventArgs"/> class.
        /// </summary>
        /// <param name="fileNumber">The index of the file in the list of files to ingest.</param>
        public PreFileIngestedEventArgs(int fileNumber)
        {
            FileNumber = fileNumber;
        }
    }
}
