using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using TFSArtifactManager.DI;
using TFSArtifactManager.Plumbing;
using TFSArtifactManager.Properties;
using TFSWorkItemChangesetInfo;
using TFSWorkItemChangesetInfo.Changesets;
using TFSWorkItemChangesetInfo.IO;

namespace TFSArtifactManager.ViewModel
{
    public class MainViewModel : AppViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            return;
        }

        public ICommand SettingsCommand
        {
            get { return new RelayCommand(ShowSettings); }
        }

        private static void ShowSettings()
        {
            var msg = new ModalMessage<SettingsViewModel>((c, v) => { return; });
            Messenger.Default.Send(msg);
        }

        public ICommand MassDownloadCommand
        {
            get { return new RelayCommand(OpenWorkspaceAction<ProjectArtifactsViewModel>); }
        }

        public ICommand DatabasePackagerCommand
        {
            get { return new RelayCommand<DatabasePackagerViewModel>(OpenDatabasePackager); }
        }

        private void OpenDatabasePackager(DatabasePackagerViewModel vm)
        {
            OpenWorkspaceItem(vm);
        }

        public ICommand ChangesetDownloadCommand
        {
            get { return new RelayCommand(ChangesetDownload); }
        }

        private void ChangesetDownload()
        {
            if (string.IsNullOrWhiteSpace(Settings.Default.TfsServerName))
            {
                if (IoC.Get<IMessageBoxService>().ShowOkCancel("TFS Server must be specified. Enter now?", "Required Data"))
                    SettingsCommand.Execute(null);
            }

            if (string.IsNullOrWhiteSpace(Settings.Default.TfsServerName))
                return;

            //TODO: save last work item id in settings and pass in a default?
            //TODO: work item id validation?
            var vm = new WorkItemSelectorViewModel();
            var msg = new ModalMessage<WorkItemSelectorViewModel>(
                vm, (confirm, resultVm) =>
                    {
                        if (!confirm || !resultVm.WorkItemId.HasValue) return;

                        try
                        {
                            this.BusyText = "Getting work item info from TFS";
                            this.IsBusy = true;
                            //TODO: use command pattern for this
                            var known = IoC.Get<KnownFileTypes>();
                            var csInfo = new ChangesetInfo(Settings.Default.TfsServerName, known);
                            csInfo.GetInfo(resultVm.WorkItemId.Value);
                            this.BusyText = "Downloading work item files";
                            csInfo.Downloader.DownloadAllFiles();
                            csInfo.Downloader.OpenWorkItemDirectory();
                        }
                        finally
                        {
                            this.IsBusy = false;
                        }
                    });
            Messenger.Default.Send(msg);
        }
        
        #region Workspaces

        private ObservableCollection<WorkspaceViewModel> _workspaces;

        /// <summary>
        /// Returns the collection of available workspaces to display.
        /// A 'workspace' is a ViewModel that can request to be closed.
        /// </summary>
        public ObservableCollection<WorkspaceViewModel> Workspaces
        {
            get
            {
                if (_workspaces == null)
                {
                    _workspaces = new ObservableCollection<WorkspaceViewModel>();
                    _workspaces.CollectionChanged += OnWorkspacesChanged;
                }
                return _workspaces;
            }
        }

        private void OnWorkspacesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems.Count != 0)
                foreach (WorkspaceViewModel workspace in e.NewItems)
                    workspace.RequestClose += OnWorkspaceRequestClose;

            if (e.OldItems != null && e.OldItems.Count != 0)
                foreach (WorkspaceViewModel workspace in e.OldItems)
                    workspace.RequestClose -= OnWorkspaceRequestClose;
        }

        private void OnWorkspaceRequestClose(object sender, EventArgs e)
        {
            var workspace = sender as WorkspaceViewModel;
            //workspace.Dispose();
            Workspaces.Remove(workspace);
        }

        private void SetActiveWorkspace(WorkspaceViewModel workspace)
        {
            //Debug.Assert(this.Workspaces.Contains(workspace));

            var collectionView = CollectionViewSource.GetDefaultView(Workspaces);
            if (collectionView != null)
                collectionView.MoveCurrentTo(workspace);
        }

        private void OpenWorkspaceAction<T>()
            where T: WorkspaceViewModel, new()
        {
            OpenWorkspaceItem<T>();
        }

        public T OpenWorkspaceItem<T>(T vm = null, bool allowMulti = false)
            where T: WorkspaceViewModel, new()
        {
            var workspaceVm = this.Workspaces.FirstOrDefault(x => x is T);

            if (null == workspaceVm || allowMulti)
            {
                //workspaceVm = vm ?? new T();
                workspaceVm = vm ?? IoC.Get<T>();
                this.Workspaces.Add(workspaceVm);
            }

            SetActiveWorkspace(workspaceVm);

            return (T)workspaceVm;
        }

        #endregion Workspaces
     
    }
}