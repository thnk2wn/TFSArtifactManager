namespace TFSWorkItemChangesetInfo.Changesets.MassDownload
{
    public class MassDownloadProgress
    {
        public string Message { get; internal set; }

        public string ItemTitle { get; internal set; }

        public string ItemType { get; internal set; }

        public int ItemId { get; internal set; }

        public string ItemBy { get; internal set; }

        public string ItemTypeIdBy
        {
            get 
            { 
                if (null != this.ItemType && null != this.ItemBy && this.ItemId > 0)
                {
                    return this.ItemType + " " + this.ItemId + " by " + this.ItemBy;
                }
                return null;
            }
        }

        public string Stage { get; internal set; }

        public bool Cancel { get; set; }
    }
}
