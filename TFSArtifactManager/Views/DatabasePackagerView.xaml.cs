using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TFSArtifactManager.Plumbing;
using TFSArtifactManager.Properties;
using TFSArtifactManager.ViewModel;
using System.Linq;
using TFSWorkItemChangesetInfo.Database;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace TFSArtifactManager.Views
{
    /// <summary>
    /// Interaction logic for DatabaseDeployerView.xaml
    /// </summary>
    public partial class DatabasePackagerView //: UserControl
    {
        private ListViewDragDropManager<DatabaseChange> _dragMgr;

        public DatabasePackagerView()
        {
            InitializeComponent();
            _dragMgr = new ListViewDragDropManager<DatabaseChange>(uxIncludedListView);
            this.Loaded += DatabasePackagerView_Loaded;
            uxDatabaseTypeCombo.SelectionChanged += uxDatabaseTypeCombo_SelectionChanged;
        }

        private void uxDatabaseTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (null != uxDatabaseTypeCombo.SelectedValue)
            {
                Settings.Default.DbPackageDbType = (int) uxDatabaseTypeCombo.SelectedValue;
                Settings.Default.Save();
            }
        }

        private void DatabasePackagerView_Loaded(object sender, RoutedEventArgs e)
        {
            uxDatabaseTypeCombo.SelectedValue = (SqlViews) Settings.Default.DbPackageDbType;
        }

        private void uxOpenButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog {Filter = "XML Files (*.xml)|*.xml"};
            var result = dlg.ShowDialog();

            if (result.HasValue && !string.IsNullOrEmpty(dlg.FileName))
            {
                this.ViewModel.OpenCommand.Execute(dlg.FileName);
            }
        }

        private DatabasePackagerViewModel ViewModel
        {
            get { return (DatabasePackagerViewModel) this.DataContext; }
        }

        private void uxExcludedListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is DatabaseChange)
                e.AddedItems.Cast<DatabaseChange>().ToList().ForEach(x=> ViewModel.ExcludedSelections.Add(x));

            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is DatabaseChange)
                e.RemovedItems.Cast<DatabaseChange>().ToList().ForEach(x => ViewModel.ExcludedSelections.Remove(x));

            uxExcludeSelectAllButton.IsEnabled = uxExcludedListView.Items.Count > 0;
        }

        private void uxIncludedListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is DatabaseChange)
                e.AddedItems.Cast<DatabaseChange>().ToList().ForEach(x => ViewModel.IncludedSelections.Add(x));

            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is DatabaseChange)
                e.RemovedItems.Cast<DatabaseChange>().ToList().ForEach(x => ViewModel.IncludedSelections.Remove(x));

            uxIncludeSelectAllButton.IsEnabled = uxIncludedListView.Items.Count > 0;
        }

        private void uxSaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog { Filter = "XML Files (*.xml)|*.xml", FileName = "DatabaseChanges.xml"};
            var result = dlg.ShowDialog();

            if (result.Value && !string.IsNullOrEmpty(dlg.FileName))
            {
                this.ViewModel.SaveAsCommand.Execute(dlg.FileName);
            }
        }

        private void File_Click(object sender, RoutedEventArgs e)
        {
            // I gave up on MVVM for this feature. it really is a UI only function really to view a sql file
            // view first nature of this app was causing problems (opposed to ViewModel first) as well as 
            // the tabbed workspaces. might return to later when more time.
            var dbChange = (DatabaseChange)((FrameworkElement)e.OriginalSource).DataContext;
            OpenFile(dbChange);
        }

        private void OpenFile(DatabaseChange change)
        {
            var win = new SqlEditorWindow { Title = change.File };
            win.Editor.SqlView = (SqlViews) Enum.Parse(typeof (SqlViews), uxDatabaseTypeCombo.SelectedValue.ToString());
            win.Editor.IsReadOnly = true;
            win.Editor.OpenFile(change.Filename);
            win.Show();
        }

        private void ListView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var listView = ((ListView) sender);

            if (e.Key == Key.Space)
                listView.SelectedItems.OfType<DatabaseChange>().ToList().ForEach(OpenFile);
            else if (e.Key == Key.Insert && listView == uxExcludedListView && this.ViewModel.IncludeSelectedCommand.CanExecute(null))
                this.ViewModel.IncludeSelectedCommand.Execute(null);
            else if (e.Key == Key.Delete && listView == uxIncludedListView && this.ViewModel.ExcludeSelectedCommand.CanExecute(null))
                this.ViewModel.ExcludeSelectedCommand.Execute(null);
            else if (e.Key == Key.Up && Keyboard.Modifiers == ModifierKeys.Control && listView == uxIncludedListView 
                && this.ViewModel.MoveUpCommand.CanExecute(null))
                this.ViewModel.MoveUpCommand.Execute(null);
            else if (e.Key == Key.Down && Keyboard.Modifiers == ModifierKeys.Control && listView == uxIncludedListView
                && this.ViewModel.MoveDownCommand.CanExecute(null))
                this.ViewModel.MoveDownCommand.Execute(null);
        }

        private void ExcludedSelectAll(object sender, RoutedEventArgs e)
        {
            SelectAll(uxExcludedListView);
        }

        private void IncludedSelectAll(object sender, RoutedEventArgs e)
        {
            SelectAll(uxIncludedListView);
        }

        private void SelectAll(ListView listView)
        {
            listView.SelectAll();
            //Debug.WriteLine(listView.SelectedItems.Count);
        }
    }
}
