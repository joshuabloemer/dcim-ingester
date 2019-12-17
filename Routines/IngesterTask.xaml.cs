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
        public TaskStatus Status { get; private set; }

        private List<string> FilesToTransfer = new List<string>();
        private long TotalTransferSize = 0;

        public event EventHandler<TaskDismissEventArgs> TaskDismissed;
        private Thread BeginThread = null;

        public IngesterTask(Guid volume)
        {
            Volume = volume;
            Status = TaskStatus.Waiting;

            Visibility = Visibility.Collapsed;
            Margin = new Thickness(0, 20, 0, 0);
            InitializeComponent();
        }


        private void IngesterPage_PageDismissed(object sender, PageDismissEventArgs e)
        {
            switch (e.DismissMessage)
            {
                case "IngesterPageStart.Transfer":
                    Status = TaskStatus.Transferring;
                    bool deleteAfter = e.Extra == "delete" ? true : false;

                    // Swap out start page for transfer page to manage the transfer
                    TaskPageTransfer transferPage = new TaskPageTransfer(
                        Volume, deleteAfter, FilesToTransfer, TotalTransferSize);
                    transferPage.PageDismissed += IngesterPage_PageDismissed;
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
                    string[] directories = Directory.GetDirectories(
                        Path.Combine(GetVolumeLetter(Volume), "DCIM"));
                    if (directories.Length == 0) return;

                    foreach (string directory in directories)
                    {
                        // Ignore directory names not conforming to DCF spec
                        if (!Regex.IsMatch(Path.GetFileName(
                            directory), "^[0-9]{3}[0-9a-zA-Z]{5}$")) continue;

                        string[] files = Directory.GetFiles(directory);

                        foreach (string file in files)
                        {
                            // Ignore file names not conforming to DCF spec
                            if (!Regex.IsMatch(Path.GetFileNameWithoutExtension(
                                file), "^[0-9a-zA-Z_]{4}[0-9]{4}$")) continue;

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
                }
                catch
                {
                    TaskDismissed?.Invoke(this, new TaskDismissEventArgs(this));
                    return;
                }

                // If there are files then show the task and load start screen
                if (FilesToTransfer.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(delegate ()
                    {
                        TaskPageStart startPage = new TaskPageStart(Volume,
                            FilesToTransfer, TotalTransferSize);
                        startPage.PageDismissed += IngesterPage_PageDismissed;
                        FrameA.Navigate(startPage);

                        Visibility = Visibility.Visible;
                    });
                }
                else TaskDismissed?.Invoke(this, new TaskDismissEventArgs(this));
            });

            BeginThread.Start();
        }
        public void Stop()
        {
            if (BeginThread.IsAlive)
                BeginThread.Abort();
        }
    }
}
