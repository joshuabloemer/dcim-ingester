using DCIMIngester.Routines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using static DCIMIngester.Routines.Helpers;

namespace DCIMIngester.Ingester
{
    public partial class IngesterTask : UserControl
    {
        public Guid Volume { get; private set; }
        private readonly string VolumeLetter;
        private readonly string VolumeLabel;
        public TaskStatus Status { get; private set; }

        private readonly List<string> FilesToTransfer = new List<string>();
        private long TotalTransferSize = 0;

        public event EventHandler<TaskDismissEventArgs> TaskDismissed;

        public IngesterTask(Guid volume)
        {
            Volume = volume;
            VolumeLetter = GetVolumeLetter(volume);
            VolumeLabel = GetVolumeLabel(volume);
            Status = TaskStatus.Discovering;

            Visibility = Visibility.Collapsed;
            Margin = new Thickness(0, 20, 0, 0);
            InitializeComponent();
        }


        private void SetStatus(TaskStatus status)
        {
            Status = status;
        }

        public void DiscoverFiles()
        {
            new Thread(delegate ()
            {
                try
                {
                    string[] directories = Directory
                        .GetDirectories(Path.Combine(VolumeLetter, "DCIM"));

                    foreach (string directory in directories)
                    {
                        // Ignore directory names not conforming to DCF spec
                        if (!Regex.IsMatch(Path.GetFileName(directory),
                            "^([1-8][0-9]{2}|9[0-8][0-9]|99[0-9])[0-9a-zA-Z_]{5}$"))
                        { continue; }

                        foreach (string file in Directory.GetFiles(directory))
                        {
                            // Ignore file names not conforming to DCF spec
                            if (!Regex.IsMatch(Path.GetFileNameWithoutExtension(file),
                                "^[0-9a-zA-Z][0-9a-zA-Z_]{3}0*([1-9]|[1-8][0-9]|9[0-9]|"
                                + "[1-8][0-9]{2}|9[0-8][0-9]|99[0-9]|[1-8][0-9]{3}|"
                                + "9[0-8][0-9]{2}|99[0-8][0-9]|999[0-9])$"))
                            { continue; }

                            TotalTransferSize += new FileInfo(file).Length;
                            FilesToTransfer.Add(file);
                        }
                    }

                    Application.Current.Dispatcher.Invoke(delegate ()
                    {
                        // If there are files then show the task and load start screen
                        if (FilesToTransfer.Count > 0)
                        {
                            Status = TaskStatus.Waiting;
                            TaskPageStart startPage = new TaskPageStart(VolumeLabel,
                                FilesToTransfer, TotalTransferSize);
                            startPage.PageDismissed += IngesterTaskPage_PageDismissed;
                            FrameA.Navigate(startPage);

                            Visibility = Visibility.Visible;
                        }
                        else TaskDismissed?.Invoke(this, new TaskDismissEventArgs(this));
                    });
                }
                catch
                {
                    Application.Current.Dispatcher.Invoke(delegate ()
                    { TaskDismissed?.Invoke(this, new TaskDismissEventArgs(this)); });
                }
            }).Start();
        }
        private void IngesterTaskPage_PageDismissed(object sender, PageDismissEventArgs e)
        {
            switch (e.DismissMessage)
            {
                case "IngesterPageStart.Transfer":
                    Status = TaskStatus.Transferring;

                    // Swap out start page for transfer page to manage the transfer
                    TaskPageTransfer transferPage = new TaskPageTransfer(
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

        public enum TaskStatus
        {
            Discovering, Waiting, Transferring, Completed, Failed, Cancelled
        };
    }
}
