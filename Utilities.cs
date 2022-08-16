using System;
using System.IO;
using MetadataExtractor.Formats.Exif;
using System.Collections.Generic;
using System.Linq;

namespace DcimIngester
{
    public static class Utilities
    {
        /// <summary>
        /// Formats a numerical storage size into a string with units based on the magnitude of the value.
        /// </summary>
        /// <param name="bytes">The storage size in bytes.</param>
        /// <returns>The formatted storage size with units based on the magnitude of the value.</returns>
        public static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double bytesDouble = bytes;

            int i;
            for (i = 0; i < units.Length && bytes >= 1024; i++, bytes /= 1024)
                bytesDouble = bytes / 1024.0;

            return string.Format("{0:0.#} {1}", bytesDouble, units[i]);
        }

        /// <summary>
        /// Checks if a directory exists. Differs from <see cref="Directory.Exists(string)"/> by throwing an exception
        /// if there is an error.
        /// </summary>
        /// <param name="path">The directory to check.</param>
        /// <returns><see langword="true"/> if the directory exists, <see langword="false"/> if it does not exist.</returns>
        public static bool DirectoryExists(string path)
        {
            // GetCreationTime() returns the below time if the directory does not exist and throws
            // an exception if there was an error
            return Directory.GetCreationTimeUtc(path) != new DateTime(1601, 1, 1, 0, 0, 0);
        }

        /// <summary>
        /// Checks if a file exists. Differs from <see cref="File.Exists(string)"/> by throwing an exception if there
        /// is an error.
        /// </summary>
        /// <param name="path">The file to check.</param>
        /// <returns><see langword="true"/> if the file exists, <see langword="false"/> if it does not exist.</returns>
        public static bool FileExists(string path)
        {
            // GetCreationTime() returns the below time if the file does not exist and throws an
            // exception if there was an error
            return File.GetCreationTimeUtc(path) != new DateTime(1601, 1, 1, 0, 0, 0);
        }

        /// <summary>
        /// Checks if two files are the same (name independent)
        /// </summary>
        /// <param name="path1">The path of the first file</param>
        /// <param name="path2">The path of the second file</param>
        /// <returns><see langword="true"/> if the files are identical, <see langword="false"/> if the are not.</returns>
        public static bool isSameFile(string path1, string path2)
        {
            IEnumerable<MetadataExtractor.Directory> metadata1 = MetadataExtractor.ImageMetadataReader.ReadMetadata(path1);
            IEnumerable<MetadataExtractor.Directory> metadata2 = MetadataExtractor.ImageMetadataReader.ReadMetadata(path2);
            foreach (var directories in metadata1.Zip(metadata2, Tuple.Create))
            {
                foreach(var tags in directories.Item1.Tags.Zip(directories.Item2.Tags, Tuple.Create))
                {
                    if(tags.Item1.Description != tags.Item2.Description && tags.Item1.Name != "File Name" && tags.Item1.Name != "File Modified Date")
                    {
                        return false;
                    }
                }
            } 
            return true;
        }

        /// <summary>
        /// Copies a file to a directory. If the file already exists in the directory then a counter is added to the
        /// file name.
        /// </summary>
        /// <param name="sourcePath">The file to copy.</param>
        /// <param name="destDirectory">The directory to copy the file to.</param>
        /// <param name="newPath">Contains the new path of the copied file.</param>
        /// <param name="renamed">Indicates whether the file name was changed to avoid duplication in the
        /// destination.</param>
        public static void CopyFile(string sourcePath, string destDirectory, out string newPath, out bool renamed, out bool skipped)
        {
            string fileName = Path.GetFileName(sourcePath);
            int duplicates = 0;
            bool copy = true;
            skipped = false;
            String path = Path.Combine(destDirectory, fileName);

            if (FileExists(path))
            {   
                if (!isSameFile(sourcePath,path))
                {
                    // Increment a duplicate counter until the file name does not exist
                    while (FileExists(Path.Combine(destDirectory, fileName)))
                    {
                        if (Path.GetFileNameWithoutExtension(sourcePath) != "")
                        {
                            fileName = string.Format("{0} ({1}){2}", Path.GetFileNameWithoutExtension(sourcePath),
                                ++duplicates, Path.GetExtension(sourcePath));
                        }
                        else fileName = string.Format("({0}){1}", ++duplicates, Path.GetExtension(sourcePath));
                    }
                }
                else 
                {
                    copy = false;
                    skipped = true;              
                }
            }
            string destination = Path.Combine(destDirectory, fileName);
            if (copy)
            {
                File.Copy(sourcePath, destination);
            }
            newPath = destination;
            renamed = duplicates != 0;
        }
    }
}
