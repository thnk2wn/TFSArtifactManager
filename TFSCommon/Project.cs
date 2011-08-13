using System;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.Client;

namespace TFSCommon
{
    public class Project
    {
        public static Microsoft.TeamFoundation.WorkItemTracking.Client.Project GetTfsProject(TeamFoundationServer tfsServer, string tfsProjectName)
        {
            if (tfsServer == null)
                throw new ArgumentNullException("tfsServer");
            if (string.IsNullOrEmpty(tfsProjectName))
                throw new ArgumentNullException("tfsProjectName");

            //WorkItemStore workItemStore = (WorkItemStore)tfsServer.GetService(typeof(WorkItemStore));
            // Using TeamFoundationServer.GetService can mask certain exceptions so use WorkItemStore(TeamFoundationServer) instead.
            var workItemStore = new WorkItemStore(tfsServer);
            if (workItemStore == null)
            {
                string errorMessage = string.Format("WorkItemStore was null.{0}AuthenticatedUserName: {1}{0}HasAuthenticated: {2}", Environment.NewLine, tfsServer.AuthenticatedUserName, tfsServer.HasAuthenticated.ToString());
                throw new NullReferenceException(errorMessage);
            }

            return workItemStore.Projects[tfsProjectName];
        }
    }
}
