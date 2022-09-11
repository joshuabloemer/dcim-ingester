using MetadataExtractor.Formats.Exif;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static DcimIngester.Utilities;
using DcimIngester.Rules;

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
        /// The index of the current or next file to be ingested.
        /// </summary>
        private int lastIngested = 0;

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
        /// <exception cref="InvalidOperationException">Thrown if the ingest is completed, aborted or already in
        /// progress.</exception>
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
                    // Parse rules here to avoid parsing again for each file to ingest
                    var parser = new Parser();
                    var rules = parser.Parse(File.ReadAllText(Properties.Settings.Default.Rules));

                    for (int i = lastIngested; i < Work.FilesToIngest.Count; i++)
                    {
                        string path = Work.FilesToIngest.ElementAt(i);
                        PreFileIngested?.Invoke(this, new PreFileIngestedEventArgs(i));

                        IngestFile(path, rules, out string newPath, out bool unsorted, out bool renamed, out bool skipped);

                        if (Properties.Settings.Default.ShouldDeleteAfter)
                            File.Delete(path);

                        lastIngested++;

                        PostFileIngested?.Invoke(this,
                            new PostFileIngestedEventArgs(newPath, i, unsorted, renamed, skipped));

                        // Only abort if the file we just ingested was not the final file
                        if (abort && i < Work.FilesToIngest.Count - 1)
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
        /// <exception cref="InvalidOperationException">Thrown if the ingest isn't actively ingesting.</exception>
        public void AbortIngest()
        {
            if (Status != IngestTaskStatus.Ingesting)
            {
                throw new InvalidOperationException(
                    "Cannot abort an ingest that isn't actively ingesting.");
            }
            else abort = true;
        }

        /// <summary>
        /// Ingests a file into the appropriate destination directory based on the date in the EXIF data. If no date is
        /// available then the file is ingested into an "unsorted" directory.
        /// </summary>
        /// <param name="path">The file to ingest.</param>
        /// <param name="newPath">Contains the new path of the ingested file.</param>
        /// <param name="unsorted">Indicates whether the file was ingested into an "unsorted" directory.</param>
        /// <param name="renamed">Indicates whether the file name was changed to avoid a clash.</param>
        private static void IngestFile(string path, SyntaxNode rules, out string newPath, out bool unsorted, out bool renamed, out bool skipped)
        {
            var evaluator = new Evaluator(path);
            string destination = (string)evaluator.Evaluate(rules);
            destination = Path.Join(Properties.Settings.Default.Destination,destination);
            unsorted = false;

            destination = CreateDestination(destination);
            CopyFile(path, destination, out newPath, out renamed, out skipped);
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
                                  
            // Search all exif subdirs for date taken
            foreach (ExifSubIfdDirectory exif in metadata.OfType<ExifSubIfdDirectory>()) 
            {
                string? dto = exif?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
                if (dto != null) 
                {
                    try 
                    {
                        return DateTime.ParseExact(dto, "yyyy:MM:dd HH:mm:ss", null);
                    }
                    catch (FormatException) { return null; }
                }
            };
            return null;
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
