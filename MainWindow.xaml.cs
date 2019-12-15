using dcim_ingester.Routines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using static dcim_ingester.Routines.Helpers;
using static dcim_ingester.Routines.VolumeWatcher;

namespace dcim_ingester
{
    public partial class MainWindow : Window
    {
        private List<Guid> Volumes = new List<Guid>();
        private List<IngesterTask> Tasks = new List<IngesterTask>();

        private int MessagesToProcess = 0;
        bool isHandlingMessage = false;

        public MainWindow()
        {
            InitializeComponent();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Volumes = GetVolumes();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Register for device change messages
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            if (source != null)
            {
                source.AddHook(WindowMessageHandler);
                RegisterDeviceNotification(source.Handle);
            }
        }
        private IntPtr WindowMessageHandler(
            IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            if (!IsLoaded)
            {
                handled = false;
                return IntPtr.Zero;
            }

            // Only want to process device insertion and removal messages
            if (msg == WM_DEVICE_CHANGE &&
                (int)wparam == DBT_DEVICE_ARRIVAL || (int)wparam == DBT_DEVICE_REMOVE_COMPLETE)
            {
                DevBroadcastDeviceInterface ver = (DevBroadcastDeviceInterface)
                    Marshal.PtrToStructure(lparam, typeof(DevBroadcastDeviceInterface));

                if (ver.Name.StartsWith("\\\\?\\")) // Ignore invalid false positives
                {
                    Interlocked.Increment(ref MessagesToProcess);
                    if (!isHandlingMessage)
                    {
                        isHandlingMessage = true;

                        // Handle message in new thread to allow this method to return
                        new Thread(delegate () { VolumesChanged(); }).Start();
                    }
                }
            }

            handled = false;
            return IntPtr.Zero;
        }

        private void VolumesChanged()
        {
            List<Guid> newVolumes = GetVolumes();

            // Check for any added volumes
            foreach (Guid volume in newVolumes)
            {
                if (!Volumes.Contains(volume))
                {
                    string volumeLetter = GetVolumeLetter(volume);
                    if (Directory.Exists(Path.Combine(volumeLetter, "DCIM"))
                        && IsFilesToTransfer(volumeLetter))
                    {
                        Application.Current.Dispatcher.Invoke(delegate ()
                        { StartIngesterTask(volume); });
                    }
                }
            }

            // Check for any removed volumes
            foreach (Guid volume in Volumes)
            {
                if (!newVolumes.Contains(volume))
                {
                    Application.Current.Dispatcher.Invoke(delegate ()
                    { StopIngesterTask(volume, true); });
                }
            }

            Volumes = newVolumes;
            Interlocked.Decrement(ref MessagesToProcess);

            // Could have received another message during previous message processing
            if (MessagesToProcess == 0)
                isHandlingMessage = false;
            else VolumesChanged();
        }

        private void StartIngesterTask(Guid volume)
        {
            IngesterTask task = new IngesterTask(volume);
            new Thread(delegate ()
            {
                task.ComputeTransferList();
                if (task.FilesToTransfer.Count == 0) return;

                Application.Current.Dispatcher.Invoke(delegate ()
                {
                    task.OnTaskDismiss += Task_OnTaskDismiss;
                    Tasks.Add(task);

                    task.Margin = new Thickness(0, 20, 0, 0);
                    StackPanelTasks.Children.Add(task);

                    Height += 140;
                    Rect workArea = SystemParameters.WorkArea;
                    Left = workArea.Right - Width - 20;
                    Top = workArea.Bottom - Height - 20;
                    Show();
                });
            }).Start();
        }
        private void Task_OnTaskDismiss(object sender, TaskDismissEventArgs e)
        {
            StopIngesterTask(e.Task.Volume);
        }
        private void StopIngesterTask(Guid volume, bool onlyIfWaiting = false)
        {
            foreach (IngesterTask task in Tasks)
            {
                if (task.Volume == volume)
                {
                    // Option to only remove task if in waiting state
                    if (onlyIfWaiting && task.Status != TaskStatus.Waiting)
                        return;

                    StackPanelTasks.Children.Remove(task);
                    Tasks.Remove(task);
                    break;
                }
            }

            if (Tasks.Count == 0) Hide();
        }
    }
}
