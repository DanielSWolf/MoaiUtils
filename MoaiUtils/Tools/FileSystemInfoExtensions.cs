using System;
using System.IO;

namespace Tools {
    public static class FileSystemInfoExtensions {
        public static DirectoryInfo GetDirectoryInfo(this DirectoryInfo directoryInfo, string relativePath) {
            return new DirectoryInfo(Path.Combine(directoryInfo.FullName, relativePath));
        }

        public static FileInfo GetFileInfo(this DirectoryInfo directoryInfo, string relativePath) {
            return new FileInfo(Path.Combine(directoryInfo.FullName, relativePath));
        }

        public static string RelativeTo(this FileSystemInfo path, DirectoryInfo containingDirectory) {
            if (!path.FullName.StartsWith(containingDirectory.FullName)) {
                throw new ArgumentException(string.Format("'{0}' is not within '{1}'.", path, containingDirectory));
            }
            return path.FullName.Substring(containingDirectory.FullName.Length + 1);
        }

    }
}