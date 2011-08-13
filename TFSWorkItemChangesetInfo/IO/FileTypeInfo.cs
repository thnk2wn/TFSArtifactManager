using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace TFSWorkItemChangesetInfo.IO
{
    public class FileTypeInfo
    {
        private Dictionary<string, FileExtension> FileExtensions { get; set; }

        public FileTypeInfo()
        {
            this.FileExtensions = new Dictionary<string, FileExtension>();
        }

        public void AddFileExtensions(params FileExtension[] fileExts)
        {
            fileExts.ToList().ForEach(AddFileExtension);
        }

        public void AddFileExtension(FileExtension ext)
        {
            if (null == ext)
                throw new ArgumentNullException("ext", "extension is required");

            if (string.IsNullOrWhiteSpace(ext.Extension))
                throw new NullReferenceException("file extension must be set");

            if (string.IsNullOrWhiteSpace(ext.Name))
                throw new NullReferenceException("File extension name must be set");

            var parsedExt = GetParsedExt(ext.Extension);

            if (this.FileExtensions.ContainsKey(parsedExt))
                throw new InvalidOperationException("FileExtensions already contains key " + parsedExt);

            this.FileExtensions.Add(parsedExt, ext);
        }

        public void RemoveFileExtension(FileExtension ext)
        {
            if (null == ext)
                throw new ArgumentNullException("ext", "ext cannot be null");
            RemoveFileExtension(ext.Extension);
        }

        public void RemoveFileExtension(string fileExt)
        {
            if (string.IsNullOrWhiteSpace(fileExt))
                throw new ArgumentNullException("fileExt", "fileExt must be set");
            var parsedExt = GetParsedExt(fileExt);
            
            if (!this.Contains(parsedExt))
                throw new InvalidOperationException("Extension " + parsedExt + "is not in the collection");

            this.FileExtensions.Remove(parsedExt);
        }

        public FileExtension GetFileExtensionForFile(string filename)
        {
            var fi = new FileInfo(filename);
            return GetFileExtension(fi.Extension);
        }

        public FileExtension GetFileExtension(string ext)
        {
            var parsedExt = GetParsedExt(ext);
            var fileExt = !this.FileExtensions.ContainsKey(parsedExt) ? null : this.FileExtensions[parsedExt];
            return fileExt;
        }

        public void Clear()
        {
            this.FileExtensions.Clear();
        }

        public bool Contains(string fileExt)
        {
            var parsedExt = GetParsedExt(fileExt);
            return this.FileExtensions.ContainsKey(parsedExt);
        }

        public string TypeName { get; protected set; }
        public KnownFileType FileType { get; protected set; }

        private static string GetParsedExt(string fileExt)
        {
            return fileExt.Replace(".", string.Empty).ToUpper();
        }

        public void Load(string filename)
        {
            var xDoc = XDocument.Load(filename);
            this.TypeName = xDoc.Root.Attribute("TypeName").Value;
            this.FileType = (KnownFileType)Enum.Parse(typeof (KnownFileType), xDoc.Root.Attribute("FileType").Value);

            var xExtensions = xDoc.Root.Element("FileExtensions").Elements("FileExtension");
            this.FileExtensions = new Dictionary<string, FileExtension>();
            xExtensions.ToList().ForEach(x=>
                {
                    var ext = new FileExtension(
                        x.Attribute("Extension").Value, x.Attribute("Name").Value, x.Attribute("Category").Value);
                    this.AddFileExtension(ext);
                });
        }

        public void Save(string filename)
        {
            var xDoc = new XDocument();
            var xRoot = new XElement(
                "FileTypeInfo", new XAttribute("TypeName", this.TypeName), new XAttribute("FileType", this.FileType));

            var xExtensions = new XElement("FileExtensions");

            this.FileExtensions.ToList().ForEach(x=>
                {
                    var xExt = new XElement(
                        "FileExtension",
                        new XAttribute("Category", x.Value.Category),
                        new XAttribute("Extension", x.Value.Extension),
                        new XAttribute("Name", x.Value.Name));
                    xExtensions.Add(xExt);
                });

            xRoot.Add(xExtensions);
            xDoc.Add(xRoot);
            xDoc.Save(filename);
        }
    }

    
}
