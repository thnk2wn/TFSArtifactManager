using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace TFSWorkItemChangesetInfo.Database
{
    public class DatabaseChanges
    {
        public DatabaseChanges()
        {
            this.IncludedChanges = new ObservableCollection<DatabaseChange>();
            this.ExcludedChanges = new ObservableCollection<DatabaseChange>();
            //this.DeletedChanges = new ObservableCollection<DatabaseChange>();
            //this.IncompleteChanges = new ObservableCollection<DatabaseChange>();
        }

        public ObservableCollection<DatabaseChange> IncludedChanges { get; set; }

        public ObservableCollection<DatabaseChange> ExcludedChanges { get; set; }

        //public ObservableCollection<DatabaseChange> DeletedChanges { get; set; }

        //public ObservableCollection<DatabaseChange> IncompleteChanges { get; set; }

        public string RootDatabaseFolder { get; set; }
        public string DeletedSubDirName { get; set; }

        public int RootWorkItemId { get; set; }

        public DateTime? GeneratedAt { get; set; }
        public DateTime? LastSavedAt { get; set; }

        public bool HasChanges
        {
            get
            {
                return null != ExcludedChanges && ExcludedChanges.Any() ||
                       null != IncludedChanges && IncludedChanges.Any();
            }
        }
    }
}
