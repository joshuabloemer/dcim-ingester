using System;

namespace DcimIngester.Routines
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
