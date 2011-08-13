using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using TFSWorkItemChangesetInfo.IO;

namespace TFSWorkItemChangesetInfo.Changesets.MassDownload
{
    partial class MassDownload
    {
        private MassDownloadChangeInfo GetChangeInfo(Changeset changeset, Change change)
        {
            var ci = new MassDownloadChangeInfo
            {
                Change = change,
                Changeset = changeset,
                File = change.Item.ServerItem.Split('/').Last()
            };
            ci.FileTypeInfo = this.Config.KnownFileTypes.GetTypeForFilenameExt(ci.File);
            ci.TaskChanges = _taskChanges.Where(x => x.TaskChangeSets.Contains(changeset)).ToList();

            // i.e. DownloadPath\Database or DownloadPath\Reports
            ci.TargetDirectory = Path.Combine(this.DownloadPath, ci.FileTypeInfo.TypeName);

            var extText = ci.File.Substring(ci.File.LastIndexOf("."));
            ci.Extension = ci.FileTypeInfo.GetFileExtension(extText);

            if (ci.IsDatabase)
            {
                // i.e. DownloadPath\Database
                if (string.IsNullOrEmpty(this.Config.RootDatabasePath))
                    this.Config.RootDatabasePath = ci.TargetDirectory;

                ci.DatabaseSchema = ci.File.Substring(0, ci.File.IndexOf(".") - 0);
                // i.e. DownloadPath\Database\Schema\VIEWS
                ci.TargetDirectory = Path.Combine(ci.TargetDirectory, ci.DatabaseSchema, ci.Extension.Category);
            }

            if (ci.HasIncompleteTask)
            {
                ci.TargetDirectory = Path.Combine(ci.TargetDirectory, "_Incomplete");
            }

            if (ci.IsDeleted)
            {
                ci.TargetFilenameDeleted = Path.Combine(ci.TargetDirectory, ci.File);
                ci.TargetDirectory = Path.Combine(ci.TargetDirectory, Constants.DELETED_SUB_DIR_NAME);
            }

            DirUtility.EnsureDir(ci.TargetDirectory);
            ci.TargetFilename = Path.Combine(ci.TargetDirectory, ci.File);

            return ci;
        }

        
    }

    /// <summary>
    /// Holds some information regarding a source control change. Previously nested inner class of mass download
    /// </summary>
    internal class MassDownloadChangeInfo
    {
        public Change Change { get; set; }

        public Changeset Changeset { get; set; }

        public string File { get; set; }

        public List<WorkItemResult> TaskChanges { get; set; }

        public List<WorkItem> Tasks
        {
            get
            {
                if (null == this.TaskChanges) return null;
                return this.TaskChanges.Select(x => x.Task).ToList();
            }
        }

        public bool HasIncompleteTask
        {
            get
            {
                if (null == this.Tasks) return false;
                return this.Tasks.Any(x => x.State != "Closed");
            }
        }

        public FileTypeInfo FileTypeInfo { get; set; }

        public bool IsDatabase
        {
            get { return (null != this.FileTypeInfo && this.FileTypeInfo.FileType == KnownFileType.Database); }
        }

        public string TargetDirectory { get; set; }

        public string TargetFilename { get; set; }

        public string TargetFilenameDeleted { get; set; }

        public FileExtension Extension { get; set; }

        public bool IsDeleted
        {
            get { return null != this.Change && this.Change.ChangeType == ChangeType.Delete; }
        }

        public string DatabaseSchema { get; set; }

        public string ServerItem
        {
            get { return (null != this.Change) ? this.Change.Item.ServerItem : null; }
        }
    }
}
