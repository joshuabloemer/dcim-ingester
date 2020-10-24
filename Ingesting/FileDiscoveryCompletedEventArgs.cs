using System;

namespace DcimIngester.Ingesting
{
    public class FileDiscoveryCompletedEventArgs : EventArgs
    {
        public FileDiscoveryResult Result { get; private set; }

        public FileDiscoveryCompletedEventArgs(FileDiscoveryResult result)
        {
            Result = result;
        }

        public enum FileDiscoveryResult { FilesFound, NoFilesFound, Error }
    }
}
