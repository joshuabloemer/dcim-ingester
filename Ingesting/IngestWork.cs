﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DcimIngester.Ingesting
{
    /// <summary
    /// Represents the work to do during an ingest operation.
    /// </summary>
    public class IngestWork
    {
        /// <summary>
        /// The GUID of the volume to ingest from.
        /// </summary>
        public readonly Guid VolumeID;

        /// <summary>
        /// The letter of the volume to ingest from, followed by a colon.
        /// </summary>
        public readonly string VolumeLetter;

        /// <summary>
        /// The label of the volume to ingest from.
        /// </summary>
        public readonly string VolumeLabel;

        /// <summary>
        /// The paths of the files to ingest from the volume.
        /// </summary>
        private readonly List<string> filesToIngest = new List<string>();

        /// <summary>
        /// The paths of the files to ingest from the volume.
        /// </summary>
        public IReadOnlyCollection<string> FilesToIngest
        {
            get { return filesToIngest.AsReadOnly(); }
        }

        /// <summary>
        /// The total size of the files to ingest from the volume.
        /// </summary>
        public long TotalIngestSize { get; private set; } = 0;

        /// <summary>
        /// Indicates whether file discovery is in progress.
        /// </summary>
        private bool isDiscovering = false;

        /// <summary>
        /// Initialises a new instance of the <see cref="IngestWork"/> class.
        /// </summary>
        /// <param name="volumeId">The GUID of the volume to ingest from.</param>
        public IngestWork(Guid volumeId)
        {
            VolumeID = volumeId;

            string? letter = Utilities.GetVolumeLetter(volumeId);
            string? label = Utilities.GetVolumeLabel(volumeId);

            if (letter == null || label == null)
                throw new VolumeNotFoundException(VolumeID);
            else
            {
                VolumeLetter = letter;
                VolumeLabel = label;
            }
        }

        /// <summary>
        /// Searches the volume for files to ingest. Only files within a directory whose name conforms to the DCF
        /// specification, which in turn is within the DCIM directory, will be found. The result is placed in 
        /// <see cref="FilesToIngest"/>.
        /// </summary>
        /// <returns><see langword="true"/> if any files were found, otherwise <see langword="false"/>.</returns>
        public Task<bool> DiscoverFilesAsync()
        {
            return Task.Run(() =>
            {
                if (isDiscovering)
                {
                    throw new InvalidOperationException(
                        "Cannot execute file discovery because it is already in progress.");
                }

                try
                {
                    isDiscovering = true;
                    filesToIngest.Clear();

                    if (!Directory.Exists(Path.Combine(VolumeLetter, "DCIM")))
                        return false;

                    string[] directories = Directory.GetDirectories(Path.Combine(VolumeLetter, "DCIM"));

                    foreach (string directory in directories)
                    {
                        // Ignore directory names not conforming to DCF spec to avoid non-image directories
                        if (Regex.IsMatch(Path.GetFileName(directory),
                            "^([1-8][0-9]{2}|9[0-8][0-9]|99[0-9])[0-9a-zA-Z_]{5}$"))
                        {
                            foreach (string file in Directory.GetFiles(directory))
                            {
                                filesToIngest.Add(file);
                                TotalIngestSize += new FileInfo(file).Length;
                            }
                        }
                    }

                    isDiscovering = false;
                    return filesToIngest.Count > 0;
                }
                catch
                {
                    filesToIngest.Clear();
                    TotalIngestSize = 0;
                    isDiscovering = false;

                    throw;
                }
            });
        }
    }
}