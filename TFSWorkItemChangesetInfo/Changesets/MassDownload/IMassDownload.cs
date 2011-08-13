using System.Collections.Generic;

namespace TFSWorkItemChangesetInfo.Changesets.MassDownload
{
    internal interface IMassDownload
    {
        MassDownloadConfig Config { get; }

        List<WorkItemResult> TaskChanges { get; }
    }
}
