using TFSArtifactManager.Plumbing;
using TFSArtifactManager.ViewModel;

namespace TFSArtifactManager.Views
{
    /// <summary>
    /// Interaction logic for WorkItemSelectorView.xaml
    /// </summary>
    public partial class WorkItemSelectorView //: Window
    {
        public WorkItemSelectorView(WorkItemSelectorViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
            this.Loaded += WorkItemSelectorView_Loaded;
        }

        private void WorkItemSelectorView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            FocusHelper.Focus(uxWorkItemId); // WTH is this necessary?
        }

        private void okayButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var vm = (WorkItemSelectorViewModel) this.DataContext;
            this.DialogResult = vm.WorkItemId.HasValue;
            this.Hide();
            this.Close();
        }
    }
}
