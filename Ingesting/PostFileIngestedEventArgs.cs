using System;

namespace DcimIngester.Ingesting
{
    /// <summary>
    /// Represents event data about a file that has just been ingested.
    /// </summary>
    public class PostFileIngestedEventArgs : EventArgs
    {
        /// <summary>
        /// The path of the file.
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// The one-based index of the file in the list of files to ingest.
        /// </summary>
        public int FileNumber { get; private set; }

        /// <summary>
        /// Indicates whether the file was sorted into an "unsorted" folder.
        /// </summary>
        public bool IsUnsorted { get; private set; }

        /// <summary>
        /// Indicates whether the file was renamed to avoid a duplicate file name.
        /// </summary>
        public bool IsRenamed { get; private set; }

        /// <summary>
        /// Initialises a new instance of the <see cref="PostFileIngestedEventArgs"/> class.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <param name="fileNumber">The one-based index of the file in the list of files to ingest.</param>
        /// <param name="unsorted">Was the file sorted into an "unsorted" folder?</param>
        /// <param name="renamed">Was the file renamed to avoid a duplicate file name?</param>
        public PostFileIngestedEventArgs(string filePath, int fileNumber, bool unsorted, bool renamed)
        {
            FilePath = filePath;
            FileNumber = fileNumber;
            IsUnsorted = unsorted;
            IsRenamed = renamed;
        }
    }
}
