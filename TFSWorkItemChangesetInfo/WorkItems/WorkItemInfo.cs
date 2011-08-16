using Microsoft.TeamFoundation.WorkItemTracking.Client;
using TFSWorkItemChangesetInfo.Extensions.Microsoft.TeamFoundation.WorkItemTracking.Client_;

namespace TFSWorkItemChangesetInfo.WorkItems
{
    public class WorkItemInfo
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string AssignedTo { get; set; }
        public string State { get; set; }

        private const int MAX_TITLE = 90;

        public string AbbreviatedTitle
        {
            get
            {
                if (null != this.Title && this.Title.Length > MAX_TITLE)
                    return this.Title.Substring(0, MAX_TITLE - 3) + "...";
                return this.Title;
            }
        }

        public WorkItemInfo()
        {
            return;
        }

        internal WorkItemInfo(WorkItem workItem)
        {
            this.Id = workItem.Id;
            this.Title = workItem.Title;
            this.AssignedTo = workItem.GetAssignedTo();
            this.State = workItem.State;
        }
    }
}
