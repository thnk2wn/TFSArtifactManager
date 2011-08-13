using System;
using System.Diagnostics;
using ChangesetRetriever.Properties;
using TFSWorkItemChangesetInfo;
using TFSWorkItemChangesetInfo.Changesets;

namespace ChangesetRetriever
{
    class Program
    {
        static void Main(string[] args)
        {
            int workItemId = 0;

            if (string.IsNullOrWhiteSpace(Settings.Default.TfsServer))
            {
                Console.Error.WriteLine("Tfs Server needs to be setting in app.config first");
            }
            else
            {
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
                    var info = new ChangesetInfo(Settings.Default.TfsServer);

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
            }

            //if (Debugger.IsAttached)
            if (Environment.UserInteractive)
            {
                Console.WriteLine("\r\nDone. Press [Enter] key to quit.");
                Console.ReadLine();
            }
        }
    }
}
