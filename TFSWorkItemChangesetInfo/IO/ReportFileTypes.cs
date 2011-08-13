namespace TFSWorkItemChangesetInfo.IO
{
    public class ReportFileTypes : FileTypeInfo
    {
        public ReportFileTypes()
        {
            this.TypeName = "Reports";
            this.FileType = KnownFileType.Report;
            // removed hard-coded list of default report extensions. to be loaded via by calling app via code or file
        }
    }
}
