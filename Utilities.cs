﻿using System;
using System.IO;
using MetadataExtractor.Formats.Exif;
using System.Collections.Generic;
using System.Linq;
using FastHashes;
using System.Reflection;

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
            try
            {
                IEnumerable<MetadataExtractor.Directory> metadata1 = MetadataExtractor.ImageMetadataReader.ReadMetadata(path1);
                IEnumerable<MetadataExtractor.Directory> metadata2 = MetadataExtractor.ImageMetadataReader.ReadMetadata(path2);
                foreach (var directories in metadata1.Zip(metadata2, Tuple.Create))
                {
                    foreach (var tags in directories.Item1.Tags.Zip(directories.Item2.Tags, Tuple.Create))
                    {
                        if (tags.Item1.Description != tags.Item2.Description && tags.Item1.Name != "File Name" && tags.Item1.Name != "File Modified Date")
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (MetadataExtractor.ImageProcessingException)
            {
                FarmHash64 hashing = new FarmHash64();
                var hash1 = hashing.ComputeHash(File.ReadAllBytes(path1));
                var hash2 = hashing.ComputeHash(File.ReadAllBytes(path2));
                return hash1.SequenceEqual(hash2);
            }
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
                if (!isSameFile(sourcePath, path))
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

        /// <summary>
        /// Gets the creation time of a file from embeded Metadata if possible, falls back to the system specific properties if not.  
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>The creation time as <see langword="DateTime"/> object.</returns>
        public static DateTime GetDateTaken(string path)
        {
            // Try to read Creation Time from metadata (only works for supported image formats)
            try
            {
                IEnumerable<MetadataExtractor.Directory> metadata = MetadataExtractor.ImageMetadataReader.ReadMetadata(path);
                // Search all exif subdirs for TagDateTimeOriginal 
                foreach (ExifSubIfdDirectory exif in metadata.OfType<ExifSubIfdDirectory>())
                {
                    string? dto = exif?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
                    if (dto != null)
                        return DateTime.ParseExact(dto, "yyyy:MM:dd HH:mm:ss", null);
                }
            }
            catch (Exception ex) when (ex is MetadataExtractor.ImageProcessingException || ex is FormatException) { }

            // Fallback to Creation Time and Modification Time for all other file types
            DateTime modifiedTime = File.GetLastWriteTime(path);
            DateTime creationTime = File.GetCreationTime(path);
            return creationTime <= modifiedTime ? creationTime : modifiedTime;
        }
        static public string AssemblyGuid
        {
            get
            {
                object[] attributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);
                if (attributes.Length == 0)
                {
                    return String.Empty;
                }
                return ((System.Runtime.InteropServices.GuidAttribute)attributes[0]).Value;
            }
        }
    }
}