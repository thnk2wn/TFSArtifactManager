using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using TFSWorkItemChangesetInfo.Extensions.Microsoft.TeamFoundation.WorkItemTracking.Client_;
using TFSWorkItemChangesetInfo.IO;

namespace TFSWorkItemChangesetInfo.Changesets.MassDownload
{
    internal class TaskInfoGenerator
    {
        private IEnumerable<WorkItemResult> TaskChanges { get; set; }
        private string RootDownloadPath { get; set; }

        public TaskInfoGenerator(IEnumerable<WorkItemResult> taskChanges, string rootDownloadPath)
        {
            this.TaskChanges = taskChanges;
            this.RootDownloadPath = rootDownloadPath;
        }

        public void Generate()
        {
            OutputTaskInfo();
        }

        private void OutputTaskInfo()
        {
            var sbSummary = new StringBuilder();
            var sbCompleteSummary = new StringBuilder();
            var lineDivider = new string('=', 90);

            var closedCount = 0;
            var activeCount = 0;
            var tasksWithArtifactsCount = 0;
            var closedWithArtifactsCount = 0;
            var artifactCountClosed = 0;

            var taskDir = DirUtility.EnsureDir(this.RootDownloadPath, "Tasks");
            var deployDir = DirUtility.EnsureDir(taskDir, "Deploy");

            this.TaskChanges.OrderBy(x => x.Task.Title).ToList().ForEach(tc =>
            {
                var t = tc.Task;
                var assignedTo = t.GetAssignedTo();
                sbSummary.AppendFormat("{0}{1}", t.Title, Environment.NewLine);
                sbSummary.AppendFormat("TFS #{0} [{1}] by {2}{3}", t.Id, t.State, assignedTo, Environment.NewLine);
                var closedTask = t.State == "Closed";

                var taskDeployDir = string.Empty;

                if (closedTask)
                {
                    closedCount++;

                    if (tc.TaskFiles.Any())
                    {
                        sbCompleteSummary.AppendFormat("{0}{1}", t.Title, Environment.NewLine);
                        sbCompleteSummary.AppendFormat("TFS #{0} [{1}] by {2}{3}", t.Id, t.State, assignedTo, Environment.NewLine);

                        taskDeployDir = DirUtility.EnsureDir(deployDir, WorkItemPath(t));
                    }
                }
                else
                {
                    activeCount++;
                }

                foreach (var de in tc.TaskFiles.OrderBy(x => x.Value.File))
                {
                    var cfi = de.Value;
                    var serverPath = cfi.ServerItem;
                    var pos = cfi.ServerItem.LastIndexOf("/");

                    if (pos > -1)
                        serverPath = serverPath.Substring(0, pos);

                    var msg = string.Format("\t{0}{1}\t{2}{1}{1}", cfi.File, Environment.NewLine, serverPath);

                    if (cfi.IsDelete)
                        msg = "\t[DELETED] => " + msg;

                    sbSummary.Append(msg);

                    if (closedTask)
                    {
                        sbCompleteSummary.Append(msg);

                        var fi = new FileInfo(cfi.Filename);

                        // deleted files
                        if (fi.Exists)
                        {
                            var newTaskFilename = Path.Combine(taskDeployDir, fi.Name);
                            File.Copy(cfi.Filename, newTaskFilename);
                        }

                        artifactCountClosed++;
                    }
                }

                if (!tc.TaskFiles.Any())
                {
                    sbSummary.AppendLine("\t No artifacts found (code changes only or otherwise excluded)");
                }
                else
                {
                    tasksWithArtifactsCount++;
                    if (closedTask)
                        closedWithArtifactsCount++;
                }

                sbSummary.AppendLine(lineDivider);
                sbSummary.AppendLine();

                if (closedTask && tc.TaskFiles.Any())
                {
                    sbCompleteSummary.AppendLine(lineDivider);
                    sbCompleteSummary.AppendLine();
                }
            });

            var sbTaskList = new StringBuilder();
            var sbTaskListClosed = new StringBuilder();
            this.TaskChanges.Select(x => x.Task).OrderBy(x => x.Title).ToList().ForEach(t =>
            {
                var assignedTo = t.GetAssignedTo();
                sbTaskList.AppendFormat("{0}{1}", t.Title, Environment.NewLine);
                sbTaskList.AppendFormat("TFS #{0} [{1}] by {2}{3}{3}", t.Id, t.State, assignedTo, Environment.NewLine);

                if (t.State == "Closed")
                {
                    sbTaskListClosed.AppendFormat("{0}{1}", t.Title, Environment.NewLine);
                    sbTaskListClosed.AppendFormat("TFS #{0} [{1}] by {2}{3}{3}", t.Id, t.State, assignedTo, Environment.NewLine);
                }
            });

            var taskCountHeader = string.Format(
                "{0} Tasks. {1} Closed, {2} Incomplete", activeCount + closedCount, closedCount, activeCount);
            var summary = string.Format(
                "{0}.  {1} Tasks with artifacts{2}{2}{3}", taskCountHeader, tasksWithArtifactsCount, Environment.NewLine,
                sbSummary);

            sbTaskList.Insert(0, taskCountHeader + Environment.NewLine + Environment.NewLine);
            sbTaskListClosed.Insert(0, string.Format("{0} Closed Tasks{1}{1}", closedCount, Environment.NewLine));
            sbCompleteSummary.Insert(0, string.Format("{0} Closed Tasks with Artifacts{1}{2} Artifact Changes{1}{1}",
                closedWithArtifactsCount, Environment.NewLine, artifactCountClosed));

            OutputTaskHyperlinks(taskDir);

            var taskSummaryFile = Path.Combine(taskDir, "Artifacts by Task (All).txt");
            var closedWithArtifactsFile = Path.Combine(taskDir, "Artifacts by Task (Closed with Artifacts).txt");
            var taskListFile = Path.Combine(taskDir, "Task List (All).txt");
            var taskListClosedFile = Path.Combine(taskDir, "Task List (Closed).txt");

            File.WriteAllText(taskSummaryFile, summary);
            File.WriteAllText(closedWithArtifactsFile, sbCompleteSummary.ToString());
            File.WriteAllText(taskListFile, sbTaskList.ToString());
            File.WriteAllText(taskListClosedFile, sbTaskListClosed.ToString());

            WriteActiveTasksFile();
        }

        private void WriteActiveTasksFile()
        {
            var activeTasks = this.TaskChanges.GroupBy(x => x.Task.Id).Select(x => x.First()).Where(
                x => x.Task.State != "Closed").OrderBy(x=> x.Task.Title).ToList();
            if (!activeTasks.Any()) return;

            var sb = new StringBuilder();
            sb.AppendFormat("{0} Task(s) Not Closed{1}{1}", activeTasks.Count, Environment.NewLine);
            activeTasks.ForEach(x=>
            {
                var t = x.Task;
                sb.AppendFormat("{0}{1}", t.Title, Environment.NewLine);
                sb.AppendFormat("TFS #{0} [{1}] by {2}{3}{3}", t.Id, t.State, x.Task.GetAssignedTo(), Environment.NewLine);
            });
            
            var filename = Path.Combine(DirUtility.EnsureDir(this.RootDownloadPath, "Tasks"),
                                        "Task List (Not Closed).txt");
            File.WriteAllText(filename, sb.ToString());
        }

        private void OutputTaskHyperlinks(string taskDir)
        {
            var sbLinks = new StringBuilder();
            sbLinks.AppendLine("<html><body><h2>Task Hyperlinks</h2>");
            this.TaskChanges.Select(x => x.Task).OrderBy(x => x.Title).ToList().ForEach(t =>
            {
                var assignedTo = t.GetAssignedTo();
                sbLinks.AppendFormat("<br/><h4>{0}{1}</h4>", t.Title, Environment.NewLine);
                sbLinks.AppendFormat(
                    "<div>Task Id: {0}, Assigned To: {1}, State: {2}{3}</div><br/>", t.Id, assignedTo, t.State, Environment.NewLine);

                if (t.HyperLinkCount > 0)
                {
                    t.Links.OfType<Hyperlink>().ToList().ForEach(l => sbLinks.AppendFormat("&nbsp;&nbsp;&nbsp;<a href=\"{0}\">{0}</a><br/>{1}",
                        l.Location, Environment.NewLine));
                }
                else
                {
                    sbLinks.AppendLine("<div>No hyperlinks found</div><br/>" + Environment.NewLine);
                }

            });
            sbLinks.AppendLine("</body></html>");

            var taskLinksFile = Path.Combine(taskDir, "Task Hyperlinks.html");
            File.WriteAllText(taskLinksFile, sbLinks.ToString());
        }

        private string WorkItemPath(WorkItem item)
        {
            const int maxLen = 75;
            var title = item.Title;
            if (title.Length > maxLen)
                title = title.Substring(0, maxLen - 1) + "_";
            return string.Format("{0} - {1}", item.Id, DirUtility.SafePath(title));
        }
    }
}
