using System.IO;

namespace TFSWorkItemChangesetInfo.IO
{
    public class KnownFileTypes
    {
        public KnownFileTypes()
        {
            this.DatabaseFileTypes = new DatabaseFileTypes();
            this.ReportFileTypes = new ReportFileTypes();
            this.OtherFileTypes = new OtherFileTypes();
        }

        public DatabaseFileTypes DatabaseFileTypes { get; private set; }

        public ReportFileTypes ReportFileTypes { get; private set; }

        public OtherFileTypes OtherFileTypes { get; private set; }

        public FileTypeInfo GetTypeForFilenameExt(string filename)
        {
            var fi = new FileInfo(filename);
            return GetTypeForExtension(fi.Extension);
        }

        public FileTypeInfo GetTypeForExtension(string fileExt)
        {
            if (DatabaseFileTypes.Contains(fileExt))
                return DatabaseFileTypes;

            if (ReportFileTypes.Contains(fileExt))
                return ReportFileTypes;

            return OtherFileTypes;
        }
    }
}
