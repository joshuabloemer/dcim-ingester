using System;

namespace DcimIngester.VolumeWatching
{
    public class VolumeChangedEventArgs : EventArgs
    {
        public string VolumeLetter { get; private set; }

        public VolumeChangedEventArgs(string volumeLetter)
        {
            VolumeLetter = volumeLetter;
        }
    }
}
