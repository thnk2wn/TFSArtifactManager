using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace TFSWorkItemChangesetInfo.IO
{
    public static class DirUtility
    {
        public static string EnsureDir(params string[] paths)
        {
            var dir = (1 == paths.Count()) ? paths[0] : Path.Combine(paths);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        public static string SafePath(string input)
        {
            // quick and dirty i know
            return
                input.Replace(@"\", string.Empty).Replace("/", string.Empty).Replace(":", string.Empty).Replace(
                    "*", string.Empty).Replace("\"", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty)
                    .Replace("|", string.Empty);
        }

        // modified from http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true
        public static void DeleteDirectory(string targetDir)
        {
            var files = Directory.GetFiles(targetDir);
            var dirs = Directory.GetDirectories(targetDir);

            foreach (var file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (var dir in dirs)
            {
                DeleteDirectory(dir);
            }

            try
            {
                Directory.Delete(targetDir, false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                try
                {
                    Thread.Sleep(250);
                    // timing issue? locked? says not empty but it is in fact empty but a 2nd time works
                    Directory.Delete(targetDir, false);
                }
                catch (Exception inner)
                {
                    Trace.WriteLine(inner);
                }
            }
        }
    }
}
