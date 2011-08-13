using Microsoft.TeamFoundation.Client;

namespace TFSCommon
{
    public class Server
    {
        public static TeamFoundationServer GetTfsServer(string tfsServerName)
        {
            var teamFoundationServer = TeamFoundationServerFactory.GetServer(tfsServerName);            
            if (!teamFoundationServer.HasAuthenticated)
                teamFoundationServer.Authenticate();
            
            return teamFoundationServer;
        }
    }
}
