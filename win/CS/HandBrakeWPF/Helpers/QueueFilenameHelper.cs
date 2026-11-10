// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QueueFilenameHelper.cs" company="HandBrake Project (https://handbrake.fr)">
//   This file is part of the HandBrake source code - It may be used under the terms of the GNU General Public License.
// </copyright>
// <summary>
//   Generates unique destination paths for queue tasks.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HandBrakeWPF.Helpers
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;

    public static class QueueFilenameHelper
    {
        private static readonly Regex NumericSuffix = new Regex(
            @"^(?<name>.*)_(?<number>\d+)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Returns the original path when it is unique, otherwise appends or increments a numeric suffix.
        /// </summary>
        public static string GetUniquePath(string path, Func<string, bool> isPathUnique)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("A destination path is required.", nameof(path));
            }

            if (isPathUnique == null)
            {
                throw new ArgumentNullException(nameof(isPathUnique));
            }

            if (isPathUnique(path))
            {
                return path;
            }

            string directory = Path.GetDirectoryName(path) ?? string.Empty;
            string extension = Path.GetExtension(path);
            string filename = Path.GetFileNameWithoutExtension(path);
            string baseFilename = filename;
            int index = 0;

            Match suffix = NumericSuffix.Match(filename);
            if (suffix.Success)
            {
                baseFilename = suffix.Groups["name"].Value;
                int.TryParse(
                    suffix.Groups["number"].Value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out index);
            }

            string newPath;
            do
            {
                index = index == int.MaxValue ? 0 : index + 1;

                newPath = Path.Combine(
                    directory,
                    string.Format(CultureInfo.InvariantCulture, "{0}_{1}{2}", baseFilename, index, extension));
            }
            while (!isPathUnique(newPath));

            return newPath;
        }
    }
}
