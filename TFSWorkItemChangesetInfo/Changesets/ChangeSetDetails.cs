using System;
using System.Collections.Generic;

namespace TFSWorkItemChangesetInfo.Changesets
{
    public class ChangeSetDetails
    {
        public int Id { get; private set; }
        public string Title { get; private set; }
        public DateTime DateCreated { get; private set; }
        public List<ChangeInfo> Changes { get; set; }

        public ChangeSetDetails(int id, string title, DateTime creationDate)
        {
            this.Id = id;
            this.Title = title;
            this.DateCreated = creationDate;

            this.Changes = new List<ChangeInfo>();
        }

        public struct ChangeInfo
        {
            public DateTime ChangeDate { get; set; }
            public string ChangeType { get; set; }
            public string ChangeItem { get; set; }
        }
    }
}
