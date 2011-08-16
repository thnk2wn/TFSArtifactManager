using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using TFSWorkItemChangesetInfo.IO;
using TFSWorkItemChangesetInfo.WorkItems;

namespace TFSWorkItemChangesetInfo.Database
{
    public class DatabaseChange : INotifyPropertyChanged
    {
        public DatabaseChange()
        {
            this.TaskList = new List<WorkItemInfo>();
        }

        private string _filename;

        public string Filename
        {
            get { return _filename; }
            set
            {
                if (_filename != value)
                {
                    _filename = value;
                    this.File = null != _filename ? new FileInfo(_filename).Name : null;
                }
            }
        }

        public string FilePath { get; internal set; }

        public string File { get; private set; }

        // editable outside of library
        private string _schema;
        public string Schema
        {
            get { return _schema; }
            set
            {
                if (_schema != value)
                {
                    _schema = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("Schema"));
                }
            }
        }

        public string ServerItem { get; internal set; }

        public FileExtension Extension { get; internal set; }

        public DateTime? FirstChanged { get; internal set; }
        public DateTime? LastChanged { get; internal set; }

        public bool IsAttachment { get; internal set; }

        public bool IsManualAdd { get; set; }

        private int _index;

        public int Index
        {
            get { return _index; }
            set
            {
                if (_index != value)
                {
                    _index = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("Index"));
                }
            }
        }

        //public bool Included { get; set; }

        public string Assignees
        {
            get
            {
                // don't care to have assignees in an order that matches the task
                var assignees = this.TaskList.Select(x => x.AssignedTo).Distinct().OrderBy(x=> x);
                return String.Join(", ", assignees);
            }
        }

        /// <summary>
        /// i.e. Package Body, View, Trigger
        /// </summary>
        public string Type
        {
            get { return (null != this.Extension) ? this.Extension.Name : null; }
        }

        public ReadOnlyCollection<WorkItemInfo> Tasks
        {
            get { return new ReadOnlyCollection<WorkItemInfo>(this.TaskList); }
        }

        public ReadOnlyCollection<ChangeTypes> ChangeTypes
        {
            get {return new ReadOnlyCollection<ChangeTypes>(_changeTypes);}
        }

        public string ChangeTypesText
        {
            get
            {
                // want this to be in the order of the change not alpha order
                //return string.Join(", ", _changeTypes.OrderBy(x => x.ToString()));
                // given change type can include a ',' so separate with ';'
                return string.Join("; ", _changeTypes);
            }
        }

        private List<WorkItemInfo> TaskList { get; set; }

        private readonly List<ChangeTypes> _changeTypes = new List<ChangeTypes>();

        internal void AddChangeType(ChangeTypes changeType)
        {
            this.LastChangeType = changeType;
            if (!_changeTypes.Contains(changeType))
                _changeTypes.Add(changeType);
        }

        public ChangeTypes LastChangeType { get; private set; }

        public bool IsDeleted
        {
            get { return this.LastChangeType == Database.ChangeTypes.Delete; }
        }

        internal void AddTask(WorkItemInfo ti)
        {
            if (!this.TaskList.Any(x=> x.Id == ti.Id))
                this.TaskList.Add(ti);
        }

        public string TfsId
        {
            get 
            {
                var ids = this.TaskList.Select(x => x.Id).Distinct().OrderBy(x => x);
                return String.Join(", ", ids);
            }
        }

        public string TfsTaskTitle
        {
            get
            {
                // want this ordered by id and not title so positions of ids and titles match up when multiple
                var titles = this.TaskList.OrderBy(x=>x.Id).Select(x => x.Title).Distinct();
                return String.Join(Environment.NewLine, titles);
                //return String.Join(", ", titles);
            }
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (null != this.PropertyChanged)
                PropertyChanged(this, e);
        }

        public bool IsIncomplete
        {
            get { return this.TaskList.Any(x => x.State != "Closed"); }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public DbChangeStates ChangeState
        {
            get
            {
                if (!this.IsDeleted && !this.IsIncomplete) return DbChangeStates.Complete;
                if (this.IsDeleted && !this.IsIncomplete) return DbChangeStates.Deleted;
                if (this.IsDeleted && this.IsIncomplete) return DbChangeStates.PendingDelete;
                if (this.IsIncomplete) return DbChangeStates.Incomplete;
                return DbChangeStates.Complete;
            }
        }
    }

    

    // direct copy of TFS. don't want to have to ref tfs assembly in UX
    [Flags]
    public enum ChangeTypes
    {
        None = 1,
        Add = 2,
        Edit = 4,
        Encoding = 8,
        Rename = 16,
        Delete = 32,
        Undelete = 64,
        Branch = 128,
        Merge = 256,
        Lock = 512,
        Rollback = 1024,
        SourceRename = 2048,
    }

    public enum DbChangeStates
    {
        Complete,
        Incomplete,
        Deleted,
        PendingDelete
    }
}
