using System;

namespace DcimIngester.Ingesting
{
    /// <summary>
    /// Represents event data about a file that has just been ingested.
    /// </summary>
    public class PostFileIngestedEventArgs : EventArgs
    {
        /// <summary>
        /// The new path of the ingested file.
        /// </summary>
        public string NewFilePath { get; private set; }

        /// <summary>
        /// The index of the file in the list of files to ingest.
        /// </summary>
        public int FileNumber { get; private set; }

        /// <summary>
        /// Indicates whether the file was ingested into an "unsorted" folder.
        /// </summary>
        public bool IsUnsorted { get; private set; }

        /// <summary>
        /// Indicates whether the file was renamed to avoid a duplicate file name.
        /// </summary>
        public bool IsRenamed { get; private set; }

        /// <summary>
        /// Indicates whether the file was skipped to avoid a duplicate file name.
        /// </summary>
        public bool IsSkipped { get; private set; }

        /// <summary>
        /// Initialises a new instance of the <see cref="PostFileIngestedEventArgs"/> class.
        /// </summary>
        /// <param name="newFilePath">The new path of the ingested file.</param>
        /// <param name="fileNumber">The index of the file in the list of files to ingest.</param>
        /// <param name="unsorted">Was the file ingested into an "unsorted" folder?</param>
        /// <param name="renamed">Was the file renamed to avoid a duplicate file name?</param>
        public PostFileIngestedEventArgs(string newFilePath, int fileNumber, bool unsorted, bool renamed, bool skipped)
        {
            NewFilePath = newFilePath;
            FileNumber = fileNumber;
            IsUnsorted = unsorted;
            IsRenamed = renamed;
            IsSkipped = skipped;
        }
    }
}
