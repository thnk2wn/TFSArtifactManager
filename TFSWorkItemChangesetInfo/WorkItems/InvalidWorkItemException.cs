using System;

namespace TFSWorkItemChangesetInfo.WorkItems
{
    public class InvalidWorkItemException : ApplicationException
    {
        public InvalidWorkItemException (string message, Exception innner)
            : base(message, innner)
        {
        }
    }
}
