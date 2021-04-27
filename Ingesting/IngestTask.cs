using MetadataExtractor.Formats.Exif;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static DcimIngester.Utilities;

namespace DcimIngester.Ingesting
{
    /// <summary>
    /// Represents an ingest operation.
    /// </summary>
    public class IngestTask
    {
        /// <summary>
        /// The work to do when the ingest is executed.
        /// </summary>
        public readonly IngestWork Work;

        /// <summary>
        /// The status of the ingest.
        /// </summary>
        public IngestTaskStatus Status { get; private set; } = IngestTaskStatus.Ready;

        /// <summary>
        /// The index of the last file (in the list of files in <see cref="Work"/>) that was successfully ingested.
        /// </summary>
        private int lastIngested = -1;

        /// <summary>
        /// Indicates whether the ingest should abort.
        /// </summary>
        private bool abort = false;

        /// <summary>
        /// Occurs when the ingest of an individual file begins.
        /// </summary>
        public event EventHandler<PreFileIngestedEventArgs>? PreFileIngested;

        /// <summary>
        /// Occurs when the ingest of an individual file successfully completes.
        /// </summary>
        public event EventHandler<PostFileIngestedEventArgs>? PostFileIngested;

        /// <summary>
        /// Initialises a new instance of the <see cref="IngestTask"/> class.
        /// </summary>
        /// <param name="work">The work to do when the ingest is executed.</param>
        public IngestTask(IngestWork work)
        {
            Work = work;
        }

        /// <summary>
        /// Executes the ingest. If the ingest fails, this can be called again to attempt to continue.
        /// </summary>
        /// <returns><see langword="true"/> if all files were successfully ingested, or <see langword="false"/> if the
        /// ingest was aborted.</returns>
        public Task<bool> IngestAsync()
        {
            return Task.Run(() =>
            {
                if (Status == IngestTaskStatus.Ingesting)
                    throw new InvalidOperationException("Cannot start an already in-progress ingest.");
                else if (Status == IngestTaskStatus.Completed)
                    throw new InvalidOperationException("Cannot start a completed ingest.");
                else if (Status == IngestTaskStatus.Aborted)
                    throw new InvalidOperationException("Cannot start an aborted ingest.");

                try
                {
                    Status = IngestTaskStatus.Ingesting;

                    for (int i = lastIngested + 1; i < Work.FilesToIngest.Count; i++)
                    {
                        string path = Work.FilesToIngest.ElementAt(i);
                        PreFileIngested?.Invoke(this, new PreFileIngestedEventArgs(path, i + 1));

                        IngestFile(path, out bool unsorted, out bool renamed);

                        if (Properties.Settings.Default.ShouldDeleteAfter)
                            File.Delete(path);

                        lastIngested++;

                        PostFileIngested?.Invoke(this,
                            new PostFileIngestedEventArgs(path, i + 1, unsorted, renamed));

                        // Only abort if the file we just ingested was not the final file
                        if (abort && lastIngested < Work.FilesToIngest.Count - 1)
                        {
                            Status = IngestTaskStatus.Aborted;
                            abort = false;
                            return false;
                        }
                    }

                    Status = IngestTaskStatus.Completed;
                    return true;
                }
                catch
                {
                    Status = IngestTaskStatus.Failed;
                    abort = false;

                    throw;
                }
            });
        }

        /// <summary>
        /// Aborts the ingest. The abort takes effect once the file that is currently being ingested has finished
        /// ingesting. The ingest can only be aborted when it is actvely ingesting.
        /// </summary>
        public void AbortIngest()
        {
            if (Status != IngestTaskStatus.Ingesting)
                throw new InvalidOperationException("Cannot abort an ingest that isn't actively ingesting.");
            else abort = true;
        }

        /// <summary>
        /// Ingests a file into the appropriate destination directory based on the date in the EXIF data. If no date is
        /// available then the file is ingested into an "unsorted" directory.
        /// </summary>
        /// <param name="path">The file to ingest.</param>
        /// <param name="unsorted">Indicates whether the file was ingested into an "unsorted" directory.</param>
        /// <param name="renamed">Indicates whether the file name was changed to avoid a clash.</param>
        private static void IngestFile(string path, out bool unsorted, out bool renamed)
        {
            DateTime? dateTaken = GetDateTaken(path);
            string destination;

            if (dateTaken != null)
            {
                switch (Properties.Settings.Default.Subfolders)
                {
                    default:
                    case 0: destination = "{0:D4}\\{1:D2}\\{2:D2}"; break;
                    case 1: destination = "{0:D4}\\{0:D4}-{1:D2}-{2:D2}"; break;
                    case 2: destination = "{0:D4}-{1:D2}-{2:D2}"; break;
                }

                destination = Path.Combine(Properties.Settings.Default.Destination,
                    string.Format(destination, dateTaken?.Year, dateTaken?.Month, dateTaken?.Day));

                unsorted = false;
            }
            else
            {
                destination = Path.Combine(Properties.Settings.Default.Destination, "Unsorted");
                unsorted = true;
            }

            destination = CreateDestination(destination);
            CopyFile(path, destination, out renamed);
        }

        /// <summary>
        /// Gets the date and time an image file was taken.
        /// </summary>
        /// <param name="path">The file to read.</param>
        /// <returns>The date and time the image was taken, or <see langword="null"/> if the file does not contain that
        /// information.</returns>
        private static DateTime? GetDateTaken(string path)
        {
            IEnumerable<MetadataExtractor.Directory> metadata;
            try
            {
                metadata = MetadataExtractor.ImageMetadataReader.ReadMetadata(path);
            }
            catch (MetadataExtractor.ImageProcessingException) { return null; }

            ExifSubIfdDirectory? exif = metadata.OfType<ExifSubIfdDirectory>().SingleOrDefault();
            string? dto = exif?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);

            if (dto == null)
                return null;

            try
            {
                return DateTime.ParseExact(dto, "yyyy:MM:dd HH:mm:ss", null);
            }
            catch (FormatException) { return null; }
        }

        /// <summary>
        /// Creates a directory if it does not exist. If the directory exists but has additional text appended to the
        /// final directory in the path, it is not created. If the directory path has no parent then it is not ceated.
        /// </summary>
        /// <param name="path">The directory to create.</param>
        /// <returns>The created or already existing directory.</returns>
        private static string CreateDestination(string path)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(path);

            // No parent means we're at a root, which isn't something that can be created
            if (dirInfo.Parent == null)
                return path;

            if (DirectoryExists(dirInfo.Parent.FullName))
            {
                string[] directories = Directory.GetDirectories(dirInfo.Parent.FullName, dirInfo.Name + "*");

                if (directories.Length > 0)
                    return Path.Combine(dirInfo.Parent.FullName, new DirectoryInfo(directories[0]).Name);
            }

            return Directory.CreateDirectory(path).FullName;
        }
    }
}
