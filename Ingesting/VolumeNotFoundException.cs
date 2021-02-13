using System;

namespace DcimIngester.Ingesting
{
    public class VolumeNotFoundException : Exception
    {
        public Guid VolumeID { get; private set; }

        public VolumeNotFoundException(Guid volumeId)
            : base(string.Format("The volume {0} could not be found.", volumeId))
        {
            VolumeID = volumeId;
        }
    }
}
