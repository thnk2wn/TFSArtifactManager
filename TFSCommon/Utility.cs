using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.Client;

namespace TFSCommon
{
    public class Utility : IDisposable
    {
        // TeamFoundationServer is obsolete
        private readonly TeamFoundationServer _tfs;
        
        private WorkItemStore _workItemStore;

        #region Ctor

        private Utility() { }

        public Utility(string serverName)
        {
            this._tfs = Server.GetTfsServer(serverName);
        }

        public Utility(TeamFoundationServer server)
        {
            this._tfs = server;
        }

        #endregion

        public IEnumerable<WorkItem> GetRelatedWorkItems(int id)
        {
            return GetRelatedWorkItems(id, wi => wi.Id > 0);            
        }
        
        public IEnumerable<WorkItem> GetRelatedWorkItems(int id, Predicate<WorkItem> selectionStrategy)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be greater than zero.");

            var workItems = new List<WorkItem>();

            var targetWorkItem = Store.GetWorkItem(id);

            var links = targetWorkItem.Links.OfType<RelatedLink>();
            if (links != null)
            {
                var relatedList = new List<WorkItem>();
                links.ToList().ForEach(lnk => relatedList.Add(Store.GetWorkItem(lnk.RelatedWorkItemId)));
                workItems.AddRange(relatedList.FindAll(selectionStrategy));
            }

            return workItems;
        }

        #region Properties

        private WorkItemStore Store
        {
            get
            {
                if (this._workItemStore == null)
                    _workItemStore = (WorkItemStore)_tfs.GetService(typeof(WorkItemStore));

                return this._workItemStore;
            }
        }

        #endregion


        public void Dispose()
        {
            if (_tfs != null)
                _tfs.Dispose();
        }
    }
}
