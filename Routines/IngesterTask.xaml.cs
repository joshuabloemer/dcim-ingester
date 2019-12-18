using DCIMIngester.IngesterTaskPages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using static DCIMIngester.Routines.Helpers;

namespace DCIMIngester.Routines
{
    public partial class IngesterTask : UserControl
    {
        public Guid Volume { get; private set; }
        private string VolumeLetter;
        private string VolumeLabel;
        public TaskStatus Status { get; private set; }

        private List<string> FilesToTransfer = new List<string>();
        private long TotalTransferSize = 0;

        private Thread BeginThread = null;

        public event EventHandler<TaskDismissEventArgs> TaskDismissed;

        public IngesterTask(Guid volume)
        {
            Volume = volume;
            VolumeLetter = GetVolumeLetter(volume);
            VolumeLabel = GetVolumeLabel(volume);
            Status = TaskStatus.Searching;

            Visibility = Visibility.Collapsed;
            Margin = new Thickness(0, 20, 0, 0);
            InitializeComponent();
        }


        private void SetStatus(TaskStatus status)
        {
            Status = status;
        }

        private void IngesterTaskPage_PageDismissed(object sender, PageDismissEventArgs e)
        {
            switch (e.DismissMessage)
            {
                case "IngesterPageStart.Transfer":
                    Status = TaskStatus.Transferring;

                    // Swap out start page for transfer page to manage the transfer
                    TaskPageTransfer transferPage = new TaskPageTransfer(VolumeLetter,
                        VolumeLabel, FilesToTransfer, TotalTransferSize, SetStatus);
                    transferPage.PageDismissed += IngesterTaskPage_PageDismissed;
                    FrameA.Navigate(transferPage);
                    break;

                case "IngesterPageStart.Cancel":
                case "IngesterPageTransfer.Dismiss":
                    TaskDismissed?.Invoke(this, new TaskDismissEventArgs(this));
                    break;
            }
        }

        public void Start()
        {
            BeginThread = new Thread(delegate ()
            {
                try
                {
                    string[] directories = Directory
                        .GetDirectories(Path.Combine(VolumeLetter, "DCIM"));

                    if (directories.Length == 0)
                        TaskDismissed?.Invoke(this, new TaskDismissEventArgs(this));

                    foreach (string directory in directories)
                    {
                        // Ignore directory names not conforming to DCF spec
                        if (!Regex.IsMatch(
                            Path.GetFileName(directory), "^[0-9]{3}[0-9a-zA-Z]{5}$"))
                            continue;

                        string[] files = Directory.GetFiles(directory);

                        foreach (string file in files)
                        {
                            // Ignore file names not conforming to DCF spec
                            if (!Regex.IsMatch(Path.GetFileNameWithoutExtension(file),
                                "^[0-9a-zA-Z_]{4}[0-9]{4}$")) continue;

                            string extension = Path.GetExtension(file).ToLower();

                            // Only include files with supported extension
                            if (extension == ".jpg" || extension == ".jpeg"
                                || extension == ".cr2")
                            {
                                TotalTransferSize += new FileInfo(file).Length;
                                FilesToTransfer.Add(file);
                            }
                        }
                    }

                    // If there are files then show the task and load start screen
                    if (FilesToTransfer.Count > 0)
                    {
                        Application.Current.Dispatcher.Invoke(delegate ()
                        {
                            Status = TaskStatus.Waiting;
                            TaskPageStart startPage = new TaskPageStart(VolumeLabel,
                                FilesToTransfer, TotalTransferSize);
                            startPage.PageDismissed += IngesterTaskPage_PageDismissed;
                            FrameA.Navigate(startPage);

                            Visibility = Visibility.Visible;
                        });
                    }
                    else TaskDismissed?.Invoke(this, new TaskDismissEventArgs(this));
                }
                catch { TaskDismissed?.Invoke(this, new TaskDismissEventArgs(this)); }
            });

            BeginThread.Start();
        }
        public void StopSearching()
        {
            if (BeginThread.IsAlive)
                BeginThread.Abort();
        }

        public enum TaskStatus
        {
            Searching, Waiting, Transferring, Completed, Failed
        };
    }
}
