using System;

namespace DcimIngester.VolumeWatching
{
    /// <summary>
    /// Represents event data about a volume that has been mounted to or unmounted from the system.
    /// </summary>
    public class VolumeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The letter of the volume.
        /// </summary>
        public char VolumeLetter { get; private set; }

        /// <summary>
        /// Initialises a new instance of the <see cref="VolumeChangedEventArgs"/> class.
        /// </summary>
        /// <param name="volumeLetter">The letter of the volume.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="volumeLetter"/> is not a letter.</exception>
        public VolumeChangedEventArgs(char volumeLetter)
        {
            if (!char.IsLetter(volumeLetter))
                throw new ArgumentException(nameof(volumeLetter) + " must be a letter");

            VolumeLetter = volumeLetter;
        }
    }
}
