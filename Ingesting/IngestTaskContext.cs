using DCIMIngester.Routines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using FileDiscoveryResult = DCIMIngester.Ingesting.FileDiscoveryCompletedEventArgs.FileDiscoveryResult;

namespace DCIMIngester.Ingesting
{
    public class IngestTaskContext
    {
        public Guid VolumeID { get; private set; }
        public string VolumeLetter { get; private set; }
        public string VolumeLabel { get; private set; }

        private readonly List<string> filesToIngest = new List<string>();
        public IReadOnlyCollection<string> FilesToIngest
        {
            get { return filesToIngest.AsReadOnly(); }
        }
        public long TotalIngestSize { get; private set; } = 0;

        public event EventHandler<FileDiscoveryCompletedEventArgs> FileDiscoveryCompleted;

        public IngestTaskContext(Guid volume)
        {
            VolumeID = volume;
            VolumeLetter = Helpers.GetVolumeLetter(volume);
            VolumeLabel = Helpers.GetVolumeLabel(volume);
        }

        public void DiscoverFiles()
        {
            new Thread(() =>
            {
                try
                {
                    string[] directories = Directory.GetDirectories(Path.Combine(VolumeLetter, "DCIM"));

                    foreach (string directory in directories)
                    {
                        // Ignore directory names not conforming to DCF spec to avoid non-image directories
                        if (Regex.IsMatch(Path.GetFileName(directory),
                            "^([1-8][0-9]{2}|9[0-8][0-9]|99[0-9])[0-9a-zA-Z_]{5}$"))
                        {
                            foreach (string file in Directory.GetFiles(directory))
                            {
                                // Ignore files with names such as ".txt"
                                if (Path.GetFileNameWithoutExtension(file) == "")
                                    continue;

                                filesToIngest.Add(file);
                                TotalIngestSize += new FileInfo(file).Length;
                            }
                        }
                        else continue;
                    }

                    if (FilesToIngest.Count > 0)
                    {
                        Application.Current.Dispatcher.Invoke(() => FileDiscoveryCompleted?.Invoke(this,
                            new FileDiscoveryCompletedEventArgs(FileDiscoveryResult.FilesFound)));
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() => FileDiscoveryCompleted?.Invoke(this,
                            new FileDiscoveryCompletedEventArgs(FileDiscoveryResult.NoFilesFound)));
                    }
                }
                catch
                {
                    Application.Current.Dispatcher.Invoke(() => FileDiscoveryCompleted?.Invoke(this,
                        new FileDiscoveryCompletedEventArgs(FileDiscoveryResult.Error)));
                }
            }).Start();
        }
    }
}
