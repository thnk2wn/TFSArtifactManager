using Microsoft.TeamFoundation.WorkItemTracking.Client;
using TFSCommon;

namespace TFSWorkItemChangesetInfo.WorkItems
{
    public class TaskInfoRetriever
    {
        public static TaskInfo Get(string tfsServer, int id)
        {
            using (var util = new Utility(tfsServer))
            {
                try
                {
                    var wi = util.GetWorkItem(id);
                    return new TaskInfo(wi);
                }
                catch (DeniedOrNotExistException badWorkItemEx)
                {
                    throw new InvalidWorkItemException(badWorkItemEx.Message, badWorkItemEx);
                }
            }
        }
    }
}
