/* This source code licensed under the GNU Affero General Public License */
using System;
using System.IO;

namespace Highpoint.Sage.Utility {
    /// <summary>
    /// A Utility class for convenience operations pertaining to directories.
    /// </summary>
    public static class DirectoryOperations {

        /// <summary>
        /// Gets the named destination directory under the app data dir for this application, ensuring that it exists.
        /// </summary>
        /// <param name="subDir">The desired subdirectory. If this is null or empty, the argument is ignored.</param>
        /// <returns>
        /// The full path name, ending in the DirectorySeparatorChar.
        /// </returns>
        public static string GetAppDataDir(string subDir = null) {
            string moduleName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            moduleName = moduleName.Substring(moduleName.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string retval = folderPath + Path.DirectorySeparatorChar + moduleName + Path.DirectorySeparatorChar;
            if (!string.IsNullOrEmpty(subDir)) {
                subDir = subDir.Trim(new char[] { Path.DirectorySeparatorChar });
                retval = retval + subDir + Path.DirectorySeparatorChar;
            }
            if (!Directory.Exists(retval)) {
                Directory.CreateDirectory(retval);
            }
            return retval;
        }
    }
}
