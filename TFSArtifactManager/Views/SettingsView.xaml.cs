using System.Windows;

namespace TFSArtifactManager.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView //: Window
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void uxCloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
