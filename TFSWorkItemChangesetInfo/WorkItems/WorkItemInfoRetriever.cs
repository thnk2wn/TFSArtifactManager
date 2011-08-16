using Microsoft.TeamFoundation.WorkItemTracking.Client;
using TFSCommon;

namespace TFSWorkItemChangesetInfo.WorkItems
{
    public class WorkItemInfoRetriever
    {
        public static WorkItemInfo Get(string tfsServer, int id)
        {
            using (var util = new Utility(tfsServer))
            {
                try
                {
                    var wi = util.GetWorkItem(id);
                    return new WorkItemInfo(wi);
                }
                catch (DeniedOrNotExistException badWorkItemEx)
                {
                    throw new InvalidWorkItemException(badWorkItemEx.Message, badWorkItemEx);
                }
            }
        }
    }
}
