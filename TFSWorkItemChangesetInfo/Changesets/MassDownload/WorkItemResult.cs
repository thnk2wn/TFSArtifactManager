using System.Collections.Generic;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TFSWorkItemChangesetInfo.Changesets.MassDownload
{
    internal class WorkItemResult
    {
        public WorkItemResult()
        {
            this.TaskChangeSets = new List<Changeset>();
            this.TaskFiles = new Dictionary<string, ChangeFileInfo>();
        }

        public WorkItem Task { get; set; }
        public List<Changeset> TaskChangeSets { get; private set; }

        public Dictionary<string, ChangeFileInfo> TaskFiles { get; private set; }

        public void AddTaskFile(string filename, string rootDownloadPath, string serverItem, bool isDelete)
        {
            if (!this.TaskFiles.ContainsKey(filename))
            {
                var cfi = new ChangeFileInfo
                {
                    Filename = filename,
                    ServerItem = serverItem,
                    File = filename.Replace(rootDownloadPath + @"\", string.Empty),
                    IsDelete = isDelete
                };
                this.TaskFiles.Add(filename, cfi);
            }
        }
    }
}
