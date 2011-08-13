namespace TFSWorkItemChangesetInfo.IO
{
    public class FileExtension
    {
        public string Extension { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }

        public FileExtension()
        {
            return;
        }

        public FileExtension(string ext, string name, string category)
            : this()
        {
            this.Extension = ext;
            this.Name = name;
            this.Category = category;
        }
    }
}
