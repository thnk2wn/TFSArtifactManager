namespace TFSWorkItemChangesetInfo.IO
{
    public class DatabaseFileTypes : FileTypeInfo
    {
        public DatabaseFileTypes()
        {
            this.TypeName = "Database";
            this.FileType = KnownFileType.Database;
            // removed hard-coded list of default db extensions. to be loaded via by calling app via code or file
        }
    }
}
