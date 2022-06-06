using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;
using System.Management;

namespace DcimIngester.VolumeWatching
{
    /// <summary>
    /// Raises events when removable volumes are mounted to or unmounted from the system.
    /// </summary>
    public class VolumeWatcher
    {
        /// <summary>
        /// Indicates whether volume watching is in progress.
        /// </summary>
        public bool IsWatching { get; private set; } = false;

        /// <summary>
        /// The Event Watcher.
        /// </summary>
        private ManagementEventWatcher watcher;

        /// <summary>
        /// Occurs when a FAT volume is mounted to the system.
        /// </summary>
        public event EventHandler<VolumeChangedEventArgs>? VolumeAdded;

        /// <summary>
        /// Occurs when a FAT volume is unmounted from the system.
        /// </summary>
        public event EventHandler<VolumeChangedEventArgs>? VolumeRemoved;

        /// <summary>
        /// Initialises a new instance of the <see cref="VolumeWatcher"/> class.
        /// </summary>
        public VolumeWatcher(){}

        /// <summary>
        /// Starts watching for changes to the removable volumes mounted to the system.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if volume watching has already started.</exception>
        public void StartWatching()
        {
            if (IsWatching)
            {
                throw new InvalidOperationException(
                    "Cannot start volume watching because it has already started.");
            }
            // Set up the event consumer
            //==========================
            // Create event query to receive timer events
            WqlEventQuery query =
                new WqlEventQuery("__InstanceOperationEvent",
                new TimeSpan(0,0,2),
                "TargetInstance isa \"Win32_DiskDrive\"");

            // Initialize an event watcher and
            // subscribe to timer events
            watcher = new ManagementEventWatcher(query);

            // Set up a listener for events
            watcher.EventArrived += new EventArrivedEventHandler(this.HandleEvent);

            // Start listening
            watcher.Start();
            IsWatching = true;        
        }
        private void HandleEvent(object sender, EventArrivedEventArgs e) 
        {
            PropertyData pd = (e as EventArrivedEventArgs).NewEvent.Properties["TargetInstance"];
            ManagementBaseObject targetInstance = pd.Value as ManagementBaseObject;
            string physicalDrive = targetInstance["DeviceId"].ToString();
            if (targetInstance["Size"] != null) {
                // get the matching drive letter and start Dialog
                using (ManagementClass devs = new ManagementClass(@"Win32_Diskdrive"))
                {
                    ManagementObjectCollection moc = devs.GetInstances();
                    foreach (ManagementObject mo in moc)
                    {				
                        if (mo["DeviceId"].ToString() == targetInstance["DeviceId"].ToString()){
                            foreach (ManagementObject b in mo.GetRelated("Win32_DiskPartition"))
                            {
                                foreach (ManagementBaseObject c in b.GetRelated("Win32_LogicalDisk"))
                                {
                                    char volumeLetter = c["Name"].ToString()[0];
                                    new Thread(() => {
                                        VolumeAdded?.Invoke(this, new VolumeChangedEventArgs(volumeLetter));
                                        }).Start();
                                }
                            }
                        }
                    }
                }
            }
    }

        /// <summary>
        /// Stops watching for changes to the removable volumes mounted to the system.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if volume watching has already stopped.</exception>
        public void StopWatching()
        {
            if (!IsWatching)
            {
                throw new InvalidOperationException(
                    "Cannot stop volume watching because it has not started.");
            }

            watcher.Stop();
            IsWatching = false;
        }
    }
}