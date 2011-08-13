using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;
using TFSWorkItemChangesetInfo.IO;

namespace TFSWorkItemChangesetInfo.Changesets
{
    public class WorkItemDeployInfo
    {
        private readonly Dictionary<string, ChangedItemDeployInfo> _flatChanges = new Dictionary<string, ChangedItemDeployInfo>();
        private readonly Dictionary<string, ChangedItemDeployInfo> _flatDeletedChanges = new Dictionary<string, ChangedItemDeployInfo>();

        internal string WorkItemDirectory { get; set; }

        internal void AddChange(Change change, Changeset cs, string localFilename, FileTypeInfo fileTypeInfo)
        {
            var info = new ChangedItemDeployInfo
            {
                FileType = fileTypeInfo,
                LocalFilename = localFilename,
                ServerPath = change.Item.ServerItem,
                LastChangedBy = cs.Owner,
                LastChangedDate = cs.CreationDate
            };

            info.AddChangeType(change.ChangeType.ToString());
            info.AddComments(cs.Comment);

            var key = localFilename.Replace(this.WorkItemDirectory, string.Empty);

            if (change.ChangeType != ChangeType.Delete)
            {
                if (!_flatChanges.ContainsKey(key))
                {
                    _flatChanges.Add(key, info);
                }
                else
                {
                    var existing = _flatChanges[key];
                    existing.LastChangedBy = info.LastChangedBy;
                    existing.LastChangedDate = info.LastChangedDate;
                }
            }
            else
            {
                _flatDeletedChanges.Add(localFilename, info);
            }
        }

        public ReadOnlyCollection<ChangedItemDeployInfo> DatabaseChanges
        {
            get
            {
                var items = this._flatChanges.Where(x => x.Value.FileType.FileType == KnownFileType.Database).ToList();
                return new ReadOnlyCollection<ChangedItemDeployInfo>(items.Select(x=> x.Value).ToList());
            }
        }

        public ReadOnlyCollection<ChangedItemDeployInfo> ReportChanges
        {
            get
            {
                var items = this._flatChanges.Where(x => x.Value.FileType.FileType == KnownFileType.Report).ToList();
                return new ReadOnlyCollection<ChangedItemDeployInfo>(items.Select(x => x.Value).ToList());
            }
        }
    }
}
