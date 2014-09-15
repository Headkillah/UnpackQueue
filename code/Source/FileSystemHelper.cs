using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;

namespace Sfn.UnpackQueue
{
    public static class FileSystemHelper
    {
        /// <summary>
        /// Get all files with the extensions ".rar" and ".001" in the specified path and all its sub folders.
        /// </summary>
        /// <param name="path">The path to search.</param>
        /// <returns>The full paths of all the files found.</returns>
        public static List<string> GetFiles(string path)
        {
            List<string> tmp = new List<string>();
            if (Directory.Exists(path))
            {
                GetFiles(path, tmp);
            }
            else if(File.Exists(path))
            {
                tmp.Add(path);
            }

            return tmp;
        }

        /// <summary>
        /// Get all files with the extensions ".rar" and ".001" in the specified path and all its sub folders.
        /// </summary>
        /// <param name="path">The path to search.</param>
        /// <param name="collection">The collection to add the files to.</param>
        /// <returns>The full paths of all the files found.</returns>
        private static void GetFiles(string path, List<string> collection)
        {
            if (!Directory.Exists(path)) { return; }

            foreach (var extension in new string[] { "rar", "001" })
            {
                string[] files = Directory.GetFiles(path, string.Format("*.{0}", extension));
                if (files.Length > 0)
                {
                    collection.AddRange(files);
                }
            }

            string[] subFolders = Directory.GetDirectories(path);
            foreach (var folder in subFolders)
            {
                GetFiles(folder, collection);
            }
        }
    }
}
