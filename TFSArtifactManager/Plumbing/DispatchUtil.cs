using System;
using System.Windows.Threading;

namespace TFSArtifactManager.Plumbing
{
    public static class DispatchUtil
    {
        public static void SafeDispatch(Action action)
        {
            if (Dispatcher.CurrentDispatcher.CheckAccess())
            {
                // do it now on this thread 
                action.Invoke();
            }
            else
            {
                // do it on the UI thread 
                Dispatcher.CurrentDispatcher.BeginInvoke(action);
            }
        }
    }
}
