using System;
using System.Diagnostics;
using System.IO;

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
        /// Copies a file to a directory. If the file already exists in the directory then a counter is added to the
        /// file name.
        /// </summary>
        /// <param name="sourcePath">The file to copy.</param>
        /// <param name="destDirectory">The directory to copy the file to.</param>
        /// <param name="newPath">Contains the new path of the copied file.</param>
        /// <param name="renamed">Indicates whether the file name was changed to avoid duplication in the
        /// destination.</param>
        public static void CopyFile(string sourcePath, string destDirectory, out string newPath, out bool renamed)
        {
            int duplicates = 0;

            while (true)
            {
                try
                {
                    string fileName;

                    if (duplicates == 0)
                        fileName = Path.GetFileName(sourcePath);
                    else if (duplicates == 1)
                    {
                        fileName = string.Format("{0} - Copy{2}", Path.GetFileNameWithoutExtension(sourcePath),
                            duplicates, Path.GetExtension(sourcePath));
                    }
                    else
                    {
                        fileName = string.Format("{0} - Copy ({1}){2}", Path.GetFileNameWithoutExtension(sourcePath),
                            duplicates, Path.GetExtension(sourcePath));
                    }

                    string destination = Path.Combine(destDirectory, fileName);
                    File.Copy(sourcePath, destination);

                    newPath = destination;
                    renamed = duplicates != 0;
                    break;
                }
                catch (IOException ex)
                when (ex.HResult == unchecked((int)0x80070050) || ex.HResult == unchecked((int)0x80070050))
                {
                    // File already exists
                    duplicates++;
                }
            }
        }
    }
}