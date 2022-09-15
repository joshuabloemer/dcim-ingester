using MetadataExtractor.Formats.Exif;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
        /// Gets the work to do when the ingest is executed.
        /// </summary>
        public readonly IngestWork Work;

        /// <summary>
        /// Gets the status of the ingest.
        /// </summary>
        public IngestTaskStatus Status { get; private set; } = IngestTaskStatus.Ready;

        /// <summary>
        /// Gets or sets the base directory to ingest files into.
        /// </summary>
        public string DestinationDirectory { get; set; }
            = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        /// <summary>
        /// Gets or sets whether original files should be deleted after they has been successfully ingested.
        /// </summary>
        public bool DeleteAfterIngest { get; set; } = false;

        /// <summary>
        /// The index of the current or next file to be ingested.
        /// </summary>
        private int lastIngested = 0;

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
        /// <param name="cancelToken">A cancellation token that can be used to cancel the ingest.</param>
        /// <exception cref="InvalidOperationException">Thrown if the ingest is completed, aborted or already in
        /// progress.</exception>
        public Task Ingest(CancellationToken cancelToken)
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

                        if (DeleteAfterIngest)
                            File.Delete(path);

                        lastIngested++;

                        PostFileIngested?.Invoke(this,
                            new PostFileIngestedEventArgs(newPath, i, unsorted, renamed, skipped));

                        // Only abort if the file we just ingested was not the final file
                        if (cancelToken.IsCancellationRequested && i < Work.FilesToIngest.Count - 1)
                        {
                            Status = IngestTaskStatus.Aborted;
                            cancelToken.ThrowIfCancellationRequested();
                        }
                    }

                    Status = IngestTaskStatus.Completed;
                }
                catch
                {
                    Status = IngestTaskStatus.Failed;
                    throw;
                }
            }, cancelToken);
        }

        /// <summary>
        /// Ingests a file into the appropriate destination directory based on the date in the EXIF data. If no date is
        /// available then the file is ingested into an "unsorted" directory.
        /// </summary>
        /// <param name="path">The file to ingest.</param>
        /// <param name="newPath">Contains the new path of the ingested file.</param>
        /// <param name="unsorted">Indicates whether the file was ingested into an "unsorted" directory.</param>
        /// <param name="renamed">Indicates whether the file name was changed to avoid a clash.</param>
        private void IngestFile(string path, SyntaxNode rules, out string newPath, out bool unsorted, out bool renamed, out bool skipped)
        {
            var evaluator = new Evaluator(path);
            string destination = (string)evaluator.Evaluate(rules);
            
            if (evaluator.RuleMatched){
                destination = Path.Join(DestinationDirectory,destination);
                unsorted = false;
            } else {
                destination = Path.Join(DestinationDirectory,"Unsorted");
                unsorted = true;
            }
            destination = CreateDestination(destination);
            CopyFile(path, destination, out newPath, out renamed, out skipped);
        }

        /// <summary>
        /// Creates a directory if it does not exist. If the directory exists but has additional text (following a
        /// space), appended to the final directory in the path, it is not created.
        /// </summary>
        /// <param name="path">The directory to create.</param>
        /// <returns>The created or already existing directory.</returns>
        private static string CreateDestination(string path)
        {
            DirectoryInfo dirInfo = new(path);

            // No parent means we're at a root, which isn't something that can be created
            if (dirInfo.Parent == null)
                return path;

            try
            {
                string[] directories = Directory.GetDirectories(dirInfo.Parent.FullName, dirInfo.Name + " *");

                if (directories.Length > 0)
                    return Path.Combine(dirInfo.Parent.FullName, new DirectoryInfo(directories[0]).Name);
            }
            catch (DirectoryNotFoundException) { }

            return Directory.CreateDirectory(path).FullName;
        }
    }
}
