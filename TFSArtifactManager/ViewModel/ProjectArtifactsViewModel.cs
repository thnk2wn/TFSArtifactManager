using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Command;
using TFSArtifactManager.DI;
using TFSArtifactManager.Plumbing;
using TFSArtifactManager.Properties;
using TFSWorkItemChangesetInfo.Changesets.MassDownload;
using TFSWorkItemChangesetInfo.IO;
using TFSWorkItemChangesetInfo.WorkItems;

namespace TFSArtifactManager.ViewModel
{
    public class ProjectArtifactsViewModel : WorkspaceViewModel
    {
        public ProjectArtifactsViewModel(IMessageBoxService messageBoxService)
        {
            _messageBoxService = messageBoxService;
            SetupCommands();
            SetupArtifactWorker();
            LoadDefaults();
            this.FetchText = "Fetch";
        }

        private readonly IMessageBoxService _messageBoxService;

        public override string WorkspaceHeader
        {
            get { return "Mass Changeset Download"; }
        }

        private BackgroundWorker ArtifactWorker { get; set; }

        private void SetupCommands()
        {
            ProjectArtifactsCommand = new RelayCommand(GetProjectArtifacts, CanExecuteFetch);
            DbPackageCommand = new RelayCommand(OpenDatabasePackager, CanExecutePackage);
            this.CancelDownloadCommand = new RelayCommand(CancelDownload, CanCancelDownload);
            this.CheckWorkItemIdCommand = new RelayCommand(CheckWorkItemId);
        }

        private void LoadDefaults()
        {
            this.SourceControlExclusionsText = Settings.Default.MassLastSourceControlExclusions;
            this.ProjectId = Settings.Default.MassLastWorkItemId;

            if (this.ProjectId > 0)
            {
                this.CheckWorkItemIdCommand.Execute(null);
            }
        }

        private bool CanExecuteFetch()
        {
            var canExecute = this.ProjectId > 0;
            canExecute = canExecute && !IsFetching && (null != this.WorkItemTitle && WORK_ITEM_FETCH != this.WorkItemTitle);
            return canExecute;
        }

        private bool CanExecutePackage()
        {
            return (null != this.Packager);
        }

        private void OpenDatabasePackager()
        {
            IoC.Get<MainViewModel>().DatabasePackagerCommand.Execute(null);
        }

        private bool _isFetching;

        public bool IsFetching
        {
            get { return _isFetching; }
            set
            {
                if (_isFetching != value)
                {
                    _isFetching = value;
                    RaisePropertyChanged(()=> IsFetching);
                    ReCalculateCommands();
                }
            }
        }

        private string _fetchText;

        public string FetchText
        {
            get { return _fetchText; }
            set
            {
                if (_fetchText != value)
                {
                    _fetchText = value;
                    RaisePropertyChanged(() => FetchText);
                }
            }
        }

        private string _sourceControlExclusionsText;

        public string SourceControlExclusionsText
        {
            get { return _sourceControlExclusionsText; }
            set
            {
                if (_sourceControlExclusionsText != value)
                {
                    _sourceControlExclusionsText = value;
                    RaisePropertyChanged(() => SourceControlExclusionsText);
                }
            }
        }

        private void SetupArtifactWorker()
        {
            ArtifactWorker = new BackgroundWorker();
            ArtifactWorker.DoWork += ArtifactWorker_DoWork;
            ArtifactWorker.RunWorkerCompleted += ArtifactWorker_RunWorkerCompleted;
        }

        private void ArtifactWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (null != e.Error)
            {
                IsFetching = false;
                FetchText = "Fetch";

                _messageBoxService.ShowErrorDispatch(e.Error.Message, "Mass Download Error");
                return;
            }

            var downloader = (MassDownload) e.Result;
            this.Log = String.Copy(downloader.LogText);
            var dbChanges = downloader.DatabaseChanges;

            downloader.Dispose();
            
            IsFetching = false;
            FetchText = "Fetch";

            var cancelled = this.CancelPending;
            if (!cancelled && dbChanges.HasChanges)
            {
                this.Packager = IoC.Get<DatabasePackagerViewModel>();
                this.Packager.Changes = dbChanges;
                this.Packager.SaveCommand.Execute(null);
            }
            ReCalculateCommands();
            this.CancelPending = false;
        }

        private DatabasePackagerViewModel Packager { get; set; }

        private void ArtifactWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var projectId = (int) e.Argument;
            var config = new MassDownloadConfig
            {
                ProjectId = projectId,
                // TODO: TFS server should be settable in the UI. For now I just added an app setting to get around hard-code
                TfsServer = Settings.Default.TfsServerName,
                BaseDownloadDirectory = Environment.CurrentDirectory,
            };

            //TODO: not safe, should pass in args class that has source control exlcusions
            var excludeList = new List<string>();
            this.SourceControlExclusionsText.Split('\r').ToList().ForEach(x=> excludeList.Add(x.Replace("\n", string.Empty)));
            config.SourceControlExclusions = excludeList.Where(x=> !string.IsNullOrWhiteSpace(x)).ToArray();
            config.KnownFileTypes = IoC.Get<KnownFileTypes>();

            var downloader = new MassDownload();
            //using (var downloader = new MassDownload())
            //{
                downloader.ProgressAction = ProgressChanged;
                downloader.DownloadChanges(config);
                //e.Result = downloader.LogText;
            //}
            e.Result = downloader;
        }

        private void CancelDownload()
        {
            this.CancelPending = true;
            ReCalculateCommands();
        }

        private bool CancelPending { get; set; }


        private void ProgressChanged(MassDownloadProgress p)
        {
            p.Cancel = this.CancelPending;
            this.Progress = p;
        }

        private MassDownloadProgress _progress;

        public MassDownloadProgress Progress
        {
            get { return _progress; }
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    RaisePropertyChanged(() => Progress);
                }
            }
        }

        private void GetProjectArtifacts()
        {
            if (string.IsNullOrWhiteSpace(Settings.Default.TfsServerName))
            {
                if (_messageBoxService.ShowOkCancel("TFS Server name must first be specified. Enter now?", "Required Data"))
                {
                    IoC.Get<MainViewModel>().SettingsCommand.Execute(null);
                }
            }

            if (string.IsNullOrWhiteSpace(Settings.Default.TfsServerName))
                return;

            if (null == this.WorkItemTitle)
            {
                _messageBoxService.ShowOKDispatch("It does not appear you supplied a valid work item.", "Work Item Title");
                return;
            }

            IsFetching = true;
            FetchText = "Fetching...";
            Trace.WriteLine("Project: " + this.ProjectId);
            Clear();

            Settings.Default.MassLastSourceControlExclusions = this.SourceControlExclusionsText;
            Settings.Default.MassLastWorkItemId = this.ProjectId;
            Settings.Default.Save();

            ArtifactWorker.RunWorkerAsync(this.ProjectId);
        }

        private void Clear()
        {
            this.Log = null;
        }

        private int _projectId;

        public int ProjectId
        {
            get { return _projectId; }

            set
            {
                if (_projectId != value)
                {
                    _projectId = value;
                    RaisePropertyChanged(() => ProjectId);
                    ReCalculateCommands();
                    this.WorkItemTitle = null;
                    this.WorkItemIdInvalidated = true;
                }
            }
        }

        private bool WorkItemIdInvalidated { get; set; }

        private void ReCalculateCommands()
        {
            // http://stackoverflow.com/questions/6020497/wpf-v4-mvvm-light-v4-bl16-mix11-relaycommand-canexecute-doesnt-fire
            ProjectArtifactsCommand.RaiseCanExecuteChanged();
            DbPackageCommand.RaiseCanExecuteChanged();
            CancelDownloadCommand.RaiseCanExecuteChanged();
        }

        private string _log;

        public string Log
        {
            get { return _log; }

            set
            {
                if (_log != value)
                {
                    _log = value;
                    RaisePropertyChanged(()=> Log);
                }
            }
        }
        
        private bool CanCancelDownload()
        {
            return IsFetching && !CancelPending;
        }

        private const string WORK_ITEM_FETCH = "[Fetching...]";

        private void CheckWorkItemId()
        {
            if (!this.WorkItemIdInvalidated || this.ProjectId <= 0 ||string.IsNullOrWhiteSpace(Settings.Default.TfsServerName))
                return;
            
            this.WorkItemTitle = WORK_ITEM_FETCH;
            var uiTaskSch = TaskScheduler.FromCurrentSynchronizationContext();
            var task = Task.Factory.StartNew(() => TaskInfoRetriever.Get(Settings.Default.TfsServerName, this.ProjectId));

            // will not block
            Task.Factory.ContinueWhenAll(new[] {task}, compl =>
                {
                    if (null == task.Exception)
                    {
                        this.WorkItemTitle = task.Result.Title;
                        this.WorkItemIdInvalidated = false;
                    }
                    else
                    {
                        this.WorkItemTitle = null;
                        var ex = task.Exception.InnerException;
                        if (ex is InvalidWorkItemException)
                            _messageBoxService.ShowOKDispatch(ex.Message, "Invalid Work Item");
                        else
                            _messageBoxService.ShowErrorDispatch(ex.Message, "Unexpected Error");
                    }

                    ReCalculateCommands();
                },
                CancellationToken.None, TaskContinuationOptions.None, uiTaskSch);
        }

        private string _workItemTitle;

        public string WorkItemTitle
        {
            get { return _workItemTitle; }
            set
            {
                if (_workItemTitle != value)
                {
                    _workItemTitle = value;
                    RaisePropertyChanged(() => WorkItemTitle);
                }
            }
        }

        public RelayCommand ProjectArtifactsCommand { get; private set; }

        public RelayCommand DbPackageCommand { get; private set; }

        public RelayCommand CancelDownloadCommand { get; private set; }

        public RelayCommand CheckWorkItemIdCommand { get; private set; }
    }
}