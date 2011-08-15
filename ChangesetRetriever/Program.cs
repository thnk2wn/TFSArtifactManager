using System;
using System.IO;
using ChangesetRetriever.Properties;
using TFSWorkItemChangesetInfo.Changesets;
using TFSWorkItemChangesetInfo.IO;

namespace ChangesetRetriever
{
    class Program
    {
        static void Main(string[] args)
        {
            int workItemId = 0;

            if (string.IsNullOrWhiteSpace(Settings.Default.TfsServer))
            {
                Console.WriteLine("Enter TFS Server: ");
                var temp = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(temp))
                {
                    Console.Error.WriteLine("Tfs Server needs to be set to continue");
                    ExitApp();
                    return;
                }

                Settings.Default.TfsServer = temp.Trim();
                Settings.Default.Save();
                Console.WriteLine();
            }
            
            if (args.Length > 0)
                workItemId = Convert.ToInt32(args[0]);
            else
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine("Enter work item id (TFS id) then press Enter:");
                    var input = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(input))
                        workItemId = Convert.ToInt32(input.Trim());
                }
            }

            if (0 == workItemId)
            {
                Console.Error.WriteLine("WorkItem id is required.");
            }
            else
            {
                Console.WriteLine("WorkItem ID: {0}", workItemId);
                var knownFileTypes = GetKnownFileTypes();
                var info = new ChangesetInfo(Settings.Default.TfsServer, knownFileTypes);

                try
                {
                    info.GetInfo(workItemId);
                    info.Downloader.DownloadAllFiles();
                    info.Downloader.OpenWorkItemDirectory();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine(ex.ToString());
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }

            ExitApp();
        }

        private static void ExitApp()
        {
            //if (Debugger.IsAttached)
            if (Environment.UserInteractive)
            {
                Console.WriteLine("\r\nDone. Press [Enter] key to quit.");
                Console.ReadLine();
            }
        }

        private static KnownFileTypes GetKnownFileTypes()
        {
            var configPath = AppDomain.CurrentDomain.BaseDirectory;
            var known = new KnownFileTypes();
            // post-build event on TFSArtifactManager copies these over
            known.DatabaseFileTypes.Load(Path.Combine(configPath, "DatabaseFileTypes.xml"));
            known.ReportFileTypes.Load(Path.Combine(configPath, "ReportFileTypes.xml"));
            return known;
        }
    }
}
