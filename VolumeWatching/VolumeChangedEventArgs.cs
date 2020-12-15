using System;

namespace DcimIngester.VolumeWatching
{
    public class VolumeChangedEventArgs : EventArgs
    {
        public Guid VolumeID { get; private set; }

        public VolumeChangedEventArgs(Guid volumeId)
        {
            VolumeID = volumeId;
        }
    }
}
