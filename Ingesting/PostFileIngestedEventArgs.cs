using System;

namespace DcimIngester.Ingesting
{
    public class PostFileIngestedEventArgs : EventArgs
    {
        public string FilePath { get; private set; }
        public int FileNumber { get; private set; }
        public bool IsUnsorted { get; private set; }
        public bool IsRenamed { get; private set; }

        public PostFileIngestedEventArgs(string filePath, int fileNumber, bool unsorted, bool renamed)
        {
            FilePath = filePath;
            FileNumber = fileNumber;
            IsUnsorted = unsorted;
            IsRenamed = renamed;
        }
    }
}
