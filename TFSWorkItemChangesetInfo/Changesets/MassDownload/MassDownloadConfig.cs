using TFSWorkItemChangesetInfo.IO;

namespace TFSWorkItemChangesetInfo.Changesets.MassDownload
{
    public class MassDownloadConfig
    {
        public int ProjectId { get; set; }

        public string BaseDownloadDirectory { get; set; }

        public string TfsServer { get; set; }

        public string[] SourceControlExclusions { get; set; }

        public bool HasSourceControlExclusions
        {
            get
            {
                return this.SourceControlExclusions != null && this.SourceControlExclusions.Length > 0;
            }
        }

        public KnownFileTypes KnownFileTypes { get; set; }

        internal string AttachmentPath { get; set; }
        internal string DownloadPath { get; set; }
        internal string RootDatabasePath { get; set; }
    }
}
