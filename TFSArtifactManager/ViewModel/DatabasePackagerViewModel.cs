using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using TFSArtifactManager.DI;
using TFSArtifactManager.Plumbing;
using TFSArtifactManager.Properties;
using TFSWorkItemChangesetInfo;
using System.Linq;
using TFSWorkItemChangesetInfo.Database;
using TFSWorkItemChangesetInfo.IO;

namespace TFSArtifactManager.ViewModel
{
    public class DatabasePackagerViewModel : WorkspaceViewModel
    {
        public DatabasePackagerViewModel()
        {
            this.ExcludedSelections = new ObservableCollection<DatabaseChange>();
            this.ExcludedSelections.CollectionChanged += ExcludedSelections_CollectionChanged;
            this.IncludedSelections = new ObservableCollection<DatabaseChange>();
            this.IncludedSelections.CollectionChanged += IncludedSelections_CollectionChanged;
            
            this.IncludeSelectedCommand = new RelayCommand(IncludeSelected, CanInclude);
            this.ExcludeSelectedCommand = new RelayCommand(ExcludeSelected, CanExclude);
            this.SaveAsCommand = new RelayCommand<string>(SaveAs, CanSaveAs);
            this.OpenCommand = new RelayCommand<string>(Open);
            this.SaveCommand = new RelayCommand(Save);
            this.MoveUpCommand = new RelayCommand(MoveUp, CanMoveUp);
            this.MoveDownCommand = new RelayCommand(MoveDown, CanMoveDown);
            this.PackageCommand = new RelayCommand(Package, CanPackage);
            this.OpenTfsTaskCommand = new RelayCommand<RoutedEventArgs>(OpenTfsTask);
            this.OpenRootDbFolderCommand = new RelayCommand(OpenRootDbFolder);
            this.LoadDatabaseTypes();
            ReCalculateCommands();
            CalculateCounts();
        }

        private void IncludedSelections_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.ReCalculateCommands();
        }

        private void ExcludedSelections_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.ReCalculateCommands();
        }

        public override string WorkspaceHeader
        {
            get { return "Database Packager"; }
        }

        private void LoadDatabaseTypes()
        {
            this.DatabaseTypes = new ObservableCollection<SqlViews>(new[] {SqlViews.Oracle, SqlViews.SqlServer});
        }

        private ObservableCollection<SqlViews> _databaseTypes;

        public ObservableCollection<SqlViews> DatabaseTypes
        {
            get { return _databaseTypes; }
            set
            {
                if (_databaseTypes != value)
                {
                    _databaseTypes = value;
                    RaisePropertyChanged(() => DatabaseTypes);
                }
            }
        }

        private DatabaseChanges _changes;

        public DatabaseChanges Changes
        {
            get { return _changes; }
            set
            {
                if (_changes != value)
                {
                    _changes = value;
                    RaisePropertyChanged(() => Changes);
                    AdjustFilterChoices();
                    AdjustFilter();
                    SetDefaultFilter();
                    CalculateCounts();
                    RaisePropertyChanged(() => HasChanges);
                    // removed property changed wireup on each DatabaseChange
                }
            }
        }

        public bool HasChanges
        {
            get { return null != this.Changes; }
        }

        private const string ALL = "All";

        private void AdjustFilterChoices()
        {
            this.Types = (null == _changes)
                             ? new ObservableCollection<string>()
                             : new ObservableCollection<string>(_changes.ExcludedChanges.Select(x => x.Type).Distinct().OrderBy(x => x).ToList());
            this.Types.Insert(0, ALL);

            this.Schemas = (null == _changes)
                               ? new ObservableCollection<string>()
                               : new ObservableCollection<string>(_changes.ExcludedChanges.Select(x => x.Schema).Distinct().Where(
                                    x=>!string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToList());
            this.Schemas.Insert(0, ALL);

            this.States = (null == _changes)
                              ? new ObservableCollection<string>()
                              : new ObservableCollection<string>(_changes.ExcludedChanges.Select(x => x.ChangeState.ToString()).Distinct().Where(
                                  x => !string.IsNullOrEmpty(x)).OrderBy(x => x.ToString()).ToList());
            this.States.Insert(0, ALL);

            this.Assignees = (null == _changes)
                                 ? new ObservableCollection<string>()
                                 : new ObservableCollection<string>(_changes.ExcludedChanges.SelectMany(x => x.Tasks.Select(y => y.AssignedTo)).Distinct(
                                     ).OrderBy(x => x).ToList());
            this.Assignees.Insert(0, ALL);
        }

        private void SetDefaultFilter()
        {
            this.FilterState = this.States.Contains(DbChangeStates.Complete.ToString()) ? DbChangeStates.Complete.ToString() : ALL;
            this.FilterSchema = ALL;
            this.FilterType = ALL;
            this.FilterAssignee = ALL;
        }

        private ObservableCollection<string> _assignees;

        public ObservableCollection<string> Assignees
        {
            get { return _assignees; }
            set
            {
                if (_assignees != value)
                {
                    _assignees = value;
                    RaisePropertyChanged(() => Assignees);
                }
            }
        }

        private ObservableCollection<string> _types;

        public ObservableCollection<string> Types
        {
            get { return _types; }
            set
            {
                if (_types != value)
                {
                    _types = value;
                    RaisePropertyChanged(() => Types);
                }
            }
        }

        private ObservableCollection<string> _schemas;

        public ObservableCollection<string> Schemas
        {
            get { return _schemas; }
            set
            {
                if (_schemas != value)
                {
                    _schemas = value;
                    RaisePropertyChanged(() => Schemas);
                }
            }
        }

        private ObservableCollection<string> _states;

        public ObservableCollection<string> States
        {
            get { return _states; }
            set
            {
                if (_states != value)
                {
                    _states = value;
                    RaisePropertyChanged(() => States);
                }
            }
        }

        private ICollectionView _changesView;

        public ICollectionView ChangesView
        {
            get { return _changesView; }
            set
            {
                if (_changesView != value)
                {
                    _changesView = value;
                    RaisePropertyChanged(() => ChangesView);
                }
            }
        }

        private string _filterType;

        public string FilterType
        {
            get { return _filterType; }
            set
            {
                if (_filterType != value)
                {
                    _filterType = value;
                    RaisePropertyChanged(() => FilterType);
                    AdjustFilter();
                }
            }
        }

        private string _filterSchema;

        public string FilterSchema
        {
            get { return _filterSchema; }
            set
            {
                if (_filterSchema != value)
                {
                    _filterSchema = value;
                    RaisePropertyChanged(() => FilterSchema);
                    AdjustFilter();
                }
            }
        }

        private string _filterState;

        public string FilterState
        {
            get { return _filterState; }
            set
            {
                if (_filterState != value)
                {
                    _filterState = value;
                    RaisePropertyChanged(() => FilterState);
                    AdjustFilter();
                }
            }
        }

        private string _filterAssignee;

        public string FilterAssignee
        {
            get { return _filterAssignee; }
            set
            {
                if (_filterAssignee != value)
                {
                    _filterAssignee = value;
                    RaisePropertyChanged(() => FilterAssignee);
                    AdjustFilter();
                }
            }
        }

        private void AdjustFilter()
        {
            if (null == this.ChangesView)
                this.ChangesView = CollectionViewSource.GetDefaultView(this.Changes.ExcludedChanges);

            this.ChangesView.Filter = x =>
                {
                    var c = (DatabaseChange) x;
                    var result = (
                        (!IsSet(this.FilterType) || c.Type == this.FilterType) &&
                        (!IsSet(this.FilterSchema) || c.Schema == this.FilterSchema) &&
                        (!IsSet(this.FilterState) || c.ChangeState.ToString() == this.FilterState) &&
                        (!IsSet(this.FilterAssignee) || c.Assignees.Contains(this.FilterAssignee))
                        );
                    return result;
                };

            CalculateCounts();
        }

        private bool IsSet(string filterValue)
        {
            return !string.IsNullOrWhiteSpace(filterValue) && filterValue != ALL;
        }

        public RelayCommand SaveCommand { get; private set; }

        public RelayCommand<string> SaveAsCommand { get; private set; }

        public RelayCommand<string> OpenCommand { get; private set; }

        public RelayCommand IncludeSelectedCommand { get; private set; }

        public RelayCommand ExcludeSelectedCommand { get; private set; }

        public RelayCommand MoveUpCommand { get; private set; }

        public RelayCommand MoveDownCommand { get; private set; }

        public RelayCommand PackageCommand { get; private set; }

        public RelayCommand<RoutedEventArgs> OpenTfsTaskCommand { get; private set; }

        public RelayCommand OpenRootDbFolderCommand { get; private set; } 

        private void MoveUp()
        {
            Move(-1);
        }

        private void MoveDown()
        {
            Move(1);
        }

        private void Move(int offset)
        {
            var selected = this.IncludedSelections[0];
            var included = this.Changes.IncludedChanges;
            var currentIndex = included.IndexOf(selected);
            included.Move(currentIndex, currentIndex + offset);

            OrderIncludedItems();
            ReCalculateCommands();
        }

        private void OrderIncludedItems()
        {
            var included = this.Changes.IncludedChanges;
            for (int i = 0; i < included.Count; i++)
            {
                included[i].Index = i + 1;
            }
        }

        private bool CanMoveUp()
        {
            if (null == Changes || !Changes.IncludedChanges.Any() || null == IncludedSelections || 1 != IncludedSelections.Count)
                return false;

            return this.Changes.IncludedChanges.First() != this.IncludedSelections[0];
        }

        private bool CanMoveDown()
        {
            if (null == Changes || !Changes.IncludedChanges.Any() || null == IncludedSelections || 1 != IncludedSelections.Count)
                return false;

            return this.Changes.IncludedChanges.Last() != this.IncludedSelections[0];
        }

        private bool CanInclude()
        {
            var enabled = null != this.Changes && this.Changes.ExcludedChanges.Any()
                          && null != this.ExcludedSelections && this.ExcludedSelections.Any();
            return enabled;
        }

        private bool CanExclude()
        {
            var enabled = null != this.Changes && this.Changes.IncludedChanges.Any()
                          && null != this.IncludedSelections && this.IncludedSelections.Any();
            return enabled;
        }

        private bool CanSaveAs(string filename)
        {
            var enabled = null != this.Changes && !string.IsNullOrEmpty(filename) && (this.Changes.ExcludedChanges.Any()
                          || this.Changes.IncludedChanges.Any());
            return enabled;
        }

        private bool CanPackage()
        {
            return (null != this.Changes && this.Changes.IncludedChanges.Any());
        }

        private ObservableCollection<DatabaseChange> _excludedSelections;

        public ObservableCollection<DatabaseChange> ExcludedSelections
        {
            get { return _excludedSelections; }
            set
            {
                if (_excludedSelections != value)
                {
                    _excludedSelections = value;
                    RaisePropertyChanged(() => ExcludedSelections);
                    ReCalculateCommands();
                }
            }
        }

        private ObservableCollection<DatabaseChange> _includedSelections;

        public ObservableCollection<DatabaseChange> IncludedSelections
        {
            get { return _includedSelections; }
            set
            {
                if (_includedSelections != value)
                {
                    _includedSelections = value;
                    RaisePropertyChanged(() => IncludedSelections);
                    ReCalculateCommands();
                }
            }
        }

        private void SaveAs(string filename)
        {
            var rep = new DatabaseChangeRepository();
            rep.Save(this.Changes, filename);
            this.Filename = filename;
        }

        private void Save()
        {
            const string file = "DatabaseChanges.xml";
            if (string.IsNullOrEmpty(this.Filename))
                this.Filename = Path.Combine(this.Changes.RootDatabaseFolder, file);
            var isNew = !File.Exists(this.Filename);
            SaveAs(this.Filename);

            // keep a copy of what the file looked like originally in case of user modifications later
            // we will still have the original for revert or reference 
            if (isNew)
                File.Copy(this.Filename, this.Filename.Replace(file, "DatabaseChanges.generated.xml"));
        }

        private string Filename { get; set; }

        private void Open(string filename)
        {
            var rep = new DatabaseChangeRepository {KnownFileTypes = IoC.Get<KnownFileTypes>()};
            this.Changes = rep.Load(filename);
            CalculateCounts();
        }

        private void IncludeSelected()
        {
            var selected = this.ExcludedSelections.ToList();
            var selectedDeleted = selected.Where(x => x.IsDeleted).ToList();
            
            selected.Where(x=> !x.IsDeleted).ToList().ForEach(x=>
                {
                    var alreadyIncluded = this.Changes.IncludedChanges.Any(y => y.Filename == x.Filename);

                    if (!alreadyIncluded)
                    {
                        x.Index = this.Changes.IncludedChanges.Count + 1;
                        this.Changes.IncludedChanges.Add(x);
                        this.Changes.ExcludedChanges.Remove(x);
                        this.ExcludedSelections.Remove(x);
                    }
                });
            
            Save();
            CalculateCounts();
            AdjustFilterChoices();

            if (selectedDeleted.Any())
            {
                var msg = "The following items were deleted and were not included:" + Environment.NewLine + "\t" +
                          string.Join(Environment.NewLine + "\t", selectedDeleted.Select(x => x.FilePath));
                IoC.Get<IMessageBoxService>().ShowOKDispatch(msg, string.Format("{0} skipped", selectedDeleted.Count));
            }
        }

        private void ExcludeSelected()
        {
            var selected = this.IncludedSelections.ToList();
            selected.ForEach(x=>
                {
                    var alreadyThere = this.Changes.ExcludedChanges.Any(y => y.Filename == x.Filename);

                    if (!alreadyThere)
                    {
                        this.Changes.ExcludedChanges.Add(x);
                        this.Changes.IncludedChanges.Remove(x);
                    }
                });

            this.IncludedSelections.Clear();

            Save();
            CalculateCounts();
            AdjustFilterChoices();
            OrderIncludedItems();
        }

        private void CalculateCounts()
        {
            var view = (null != this.ChangesView) ? this.ChangesView.Cast<DatabaseChange>() : null;
            var filteredIn = (null != view) ? view.Count() : 0;
            var includedCount = (null != this.Changes && null != this.Changes.IncludedChanges)
                                    ? this.Changes.IncludedChanges.Count
                                    : 0;
            var excludedCount = (null != this.Changes && null != this.Changes.ExcludedChanges)
                                    ? this.Changes.ExcludedChanges.Count
                                    : 0;
            var excludeSuffix = (filteredIn == excludedCount)
                                    ? " (" + excludedCount + ")"
                                    : string.Format(" ({0} total,  {1} shown)", excludedCount, filteredIn);

            this.IncludedTitle = string.Format("Included Changes ({0})", includedCount);
            this.ExcludedTitle = string.Format("Excluded Changes{0}", excludeSuffix);
        }

        private string _excludedTitle;

        public string ExcludedTitle
        {
            get { return _excludedTitle; }
            set
            {
                if (_excludedTitle != value)
                {
                    _excludedTitle = value;
                    RaisePropertyChanged(() => ExcludedTitle);
                }
            }
        }

        private string _includedTitle;

        public string IncludedTitle
        {
            get { return _includedTitle; }
            set
            {
                if (_includedTitle != value)
                {
                    _includedTitle = value;
                    RaisePropertyChanged(() => IncludedTitle);
                }
            }
        }

        private void ReCalculateCommands()
        {
            // http://stackoverflow.com/questions/6020497/wpf-v4-mvvm-light-v4-bl16-mix11-relaycommand-canexecute-doesnt-fire
            if (null != IncludeSelectedCommand)
                IncludeSelectedCommand.RaiseCanExecuteChanged();

            if (null != this.ExcludeSelectedCommand)
                this.ExcludeSelectedCommand.RaiseCanExecuteChanged();

            if (null != this.MoveUpCommand)
                this.MoveUpCommand.RaiseCanExecuteChanged();

            if (null != this.MoveDownCommand)
                this.MoveDownCommand.RaiseCanExecuteChanged();

            if (null != SaveAsCommand)
                SaveAsCommand.RaiseCanExecuteChanged();

            if (null != PackageCommand)
                this.PackageCommand.RaiseCanExecuteChanged();
        }

        protected override void OnRequestClose()
        {
            if (null != this.Changes && (this.Changes.ExcludedChanges.Any() || this.Changes.IncludedChanges.Any()))
                this.Save();

            base.OnRequestClose();
        }

        private void Package()
        {
            this.Save();

            if (this.Changes.IncludedChanges.Any(x=> string.IsNullOrWhiteSpace(x.Schema)))
            {
                IoC.Get<IMessageBoxService>().ShowOKDispatch("One or more database objects are missing required Schema values.",
                    "Required Data Missing");
                return;
            }

            var packageDir = Path.Combine(this.Changes.RootDatabaseFolder, "_Package");
            var di = new DirectoryInfo(packageDir);

            if (di.Exists)
                DirUtility.DeleteDirectory(di.FullName);
            di.Create();            

            var schemaCounts = new Dictionary<string, int>();

            // this should be a user configurable setting. this allows gaps between changes should others need to be inserted
            const int skipBetween = 5;
            var warnings = new List<string>();

            this.Changes.IncludedChanges.OrderBy(x=> x.Index).ToList().ForEach(x=>
                {
                    var sourceFilename = Path.Combine(Changes.RootDatabaseFolder, x.FilePath);
                    var delim = !x.File.StartsWith("_") ? "_" : string.Empty;
                    var schemaDir = string.Empty;

                    if (!schemaCounts.ContainsKey(x.Schema))
                    {
                        schemaCounts.Add(x.Schema, 0);
                        schemaDir = Path.Combine(packageDir, schemaCounts.Count.ToString("00") + "_" + x.Schema);
                        DirUtility.EnsureDir(schemaDir);
                    }
                    else
                    {
                        var keys = schemaCounts.Keys.ToList();
                        var index = keys.IndexOf(x.Schema) + 1;
                        schemaDir = Path.Combine(packageDir, index.ToString("00") + "_" + x.Schema);
                    }

                    if (!File.Exists(sourceFilename))
                    {
                        warnings.Add(string.Format("The following file does not exist. Perhaps it was deleted and should not have been included: {0}", sourceFilename));
                    }
                    else
                    {
                        var schemaIndex = schemaCounts[x.Schema] * skipBetween;
                        schemaCounts[x.Schema] += 1;

                        var targetFilename = Path.Combine(schemaDir, string.Format("{0:000}{1}{2}", schemaIndex, delim, x.File));
                        File.Copy(sourceFilename, targetFilename);    
                    }
                });

            Process.Start(packageDir);

            if (warnings.Any())
            {
                var warningsFile = Path.Combine(packageDir, "PackageWarnings.txt");
                File.WriteAllText(warningsFile, string.Join(Environment.NewLine, warnings));
                Process.Start(warningsFile);
            }
        }

        private void OpenTfsTask(RoutedEventArgs e)
        {
            var btn = ((System.Windows.Controls.Button) e.OriginalSource);
            var tfsId = Convert.ToInt32(btn.Content);
            var url = string.Format(
                "http://{0}:{1}/wi.aspx?id={2}", Settings.Default.TfsServerName, Settings.Default.TfsWebUrlPort, tfsId);
            //Process.Start(url);
            Process.Start("iexplore", url);
        }

        private void OpenRootDbFolder()
        {
            Process.Start(this.Changes.RootDatabaseFolder);
        }
    }
}
