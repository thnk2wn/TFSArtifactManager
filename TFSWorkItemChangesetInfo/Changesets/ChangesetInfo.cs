using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using TFSWorkItemChangesetInfo.IO;
using Server = TFSCommon.Server;

namespace TFSWorkItemChangesetInfo.Changesets
{
    public class ChangesetInfo
    {
        private readonly VersionControlServer _versionControl;
        private readonly WorkItemStore _workItemStore;

        // deprecated / obsolete:
        private readonly TeamFoundationServer _tfs;

        private List<Changeset> TfsChangeSets { get; set; }
        private WorkItem WorkItem { get; set; }

        public ChangesetInfo(string tfsServer, KnownFileTypes knownFileTypes)
        {
            if (string.IsNullOrWhiteSpace(tfsServer))
                throw new ArgumentNullException("tfsServer", "tfsServer is required");
            _tfs = Server.GetTfsServer(tfsServer);
            _versionControl = _tfs.GetService(typeof(VersionControlServer)) as VersionControlServer;
            _workItemStore = (WorkItemStore)_tfs.GetService(typeof(WorkItemStore));
            this.Downloader = new WorkItemFileManager(knownFileTypes);
        }

        public int WorkItemId { get; private set; }

        public IEnumerable<ChangeSetDetails> GetInfo(int workItemId)
        {
            this.WorkItemId = workItemId;
            this.ChangeSets = new List<ChangeSetDetails>();
            this.TfsChangeSets = new List<Changeset>();

            WorkItem = _workItemStore.GetWorkItem(workItemId);
            if (WorkItem == null)
                throw new NullReferenceException("WorkItem");

            if (0 == WorkItem.ExternalLinkCount) return ChangeSets;

            var links = WorkItem.Links.OfType<ExternalLink>().AsEnumerable();

            foreach (var link in links)
            {
                var tfsChangeSet = _versionControl.ArtifactProvider.GetChangeset(new Uri(link.LinkedArtifactUri));
                this.TfsChangeSets.Add(tfsChangeSet);
                var cs = CreateChangeSet(tfsChangeSet);

                ((List<ChangeSetDetails>)this.ChangeSets).Add(cs);
            }

            this.Downloader.WorkItem = this.WorkItem;
            this.Downloader.Changesets = this.TfsChangeSets;
            
            return this.ChangeSets;
        }

        public IEnumerable<ChangeSetDetails> ChangeSets { get; private set; }
        
        public WorkItemFileManager Downloader { get; private set; }


        // --------------------------------------------------------------------
        // PRIVATE
        // --------------------------------------------------------------------
        private static ChangeSetDetails CreateChangeSet(Changeset tfsChangeSet)
        {
            var cs = new ChangeSetDetails(tfsChangeSet.ChangesetId, tfsChangeSet.Comment, tfsChangeSet.CreationDate);

            foreach (var tfsChange in tfsChangeSet.Changes)
            {
                cs.Changes.Add(
                    new ChangeSetDetails.ChangeInfo
                    {
                        ChangeDate = tfsChange.Item.CheckinDate,
                        ChangeType = tfsChange.ChangeType.ToString(),
                        ChangeItem = tfsChange.Item.ServerItem
                    });
            }
            return cs;
        }
    }
}