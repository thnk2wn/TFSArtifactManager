namespace TFSWorkItemChangesetInfo.Extensions.System_
{
    public static class StringExtensions
    {
        public static string RemoveAfterLast(this string value, string last)
        {
            var pos = value.LastIndexOf(last);
            var ret = value;
            if (pos > -1)
                ret = value.Substring(pos + 1);
            return ret;
        }
    }
}
