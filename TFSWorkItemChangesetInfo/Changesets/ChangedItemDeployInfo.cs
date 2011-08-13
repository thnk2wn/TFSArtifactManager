using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using TFSWorkItemChangesetInfo.IO;

namespace TFSWorkItemChangesetInfo.Changesets
{
    public class ChangedItemDeployInfo
    {
        public ChangedItemDeployInfo()
        {
            this.ChangeTypesList = new List<string>();
            _commentsBuilder = new StringBuilder();
        }

        public string LocalFilename { get; set; }

        public string ItemName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.LocalFilename))
                    return null;

                var fi = new FileInfo(this.LocalFilename);
                return fi.Name.Replace(fi.Extension, string.Empty);
            }
        }

        public string ItemExtension
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.LocalFilename))
                    return null;

                return new FileInfo(this.LocalFilename).Extension;
            }
        }

        public FileExtension FileExt
        {
            get
            {
                if (null == this.FileType) return null;
                return this.FileType.GetFileExtension(this.ItemExtension);
            }
        }

        public FileTypeInfo FileType { get; set; }

        public string ServerPath { get; set; }

        public string LastChangedBy { get; set; }

        public DateTime LastChangedDate { get; set; }

        private List<string> ChangeTypesList { get; set; }

        internal void AddChangeType(string changeType)
        {
            if (!this.ChangeTypesList.Contains(changeType))
                this.ChangeTypesList.Add(changeType);
        }

        internal void AddComments(string comments)
        {
            if (!string.IsNullOrWhiteSpace(comments))
            {
                _commentsBuilder.AppendLine();
                _commentsBuilder.Append(comments);
                _commentsBuilder.AppendLine();
            }
        }

        public ReadOnlyCollection<string> ChangeTypes
        {
            get { return new ReadOnlyCollection<string>(this.ChangeTypesList); }
        }

        private readonly StringBuilder _commentsBuilder;

        public string CombinedCheckInComments
        {
            get { return _commentsBuilder.ToString(); }
        }

        public override string ToString()
        {
            return string.Format("{0} : {1}", this.LocalFilename, this.ServerPath);
        }

    }
}
