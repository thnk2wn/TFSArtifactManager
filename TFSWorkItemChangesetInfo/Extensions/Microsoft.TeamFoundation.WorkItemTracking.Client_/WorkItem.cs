using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TFSWorkItemChangesetInfo.Extensions.Microsoft.TeamFoundation.WorkItemTracking.Client_
{
    public static class WorkItemExtensions
    {
        public static string GetAssignedTo(this WorkItem wi)
        {
            var assignedTo = wi.Fields["Assigned To"].Value.ToString();
            return assignedTo;
        }
    }
}
