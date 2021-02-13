using System;

namespace DcimIngester.Ingesting
{
    public class PreFileIngestedEventArgs : EventArgs
    {
        public string FilePath { get; private set; }
        public int FileNumber { get; private set; }

        public PreFileIngestedEventArgs(string filePath, int fileNumber)
        {
            FilePath = filePath;
            FileNumber = fileNumber;
        }
    }
}
