using System.Windows;
using System.Windows.Input;

namespace TFSArtifactManager.Views
{
    /// <summary>
    /// Description for ProjectArtifactsView.
    /// </summary>
    public partial class ProjectArtifactsView //: UserControl
    {
        /// <summary>
        /// Initializes a new instance of the ProjectArtifactsView class.
        /// </summary>
        public ProjectArtifactsView()
        {
            InitializeComponent();
        }

        private void uxIdTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // TODO: how would F# handle this?
            int numericValue = 0;

            if (!int.TryParse(e.Text, out numericValue))
                e.Handled = true;
        }

        private void TheProjectArtifactsView_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(uxIdTextBox);
            uxIdTextBox.SelectAll();
        }
    }
}