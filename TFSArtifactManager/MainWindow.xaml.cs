using System.Diagnostics;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using TFSArtifactManager.Plumbing;
using TFSArtifactManager.ViewModel;
using TFSArtifactManager.Views;

namespace TFSArtifactManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow //: Window
    {
        public MainWindow()
        {
            InitializeComponent();
            RegisterMessages();
        }

        private void RegisterMessages()
        {
            Messenger.Default.Register<string>(this, s => Debug.WriteLine(s));
            
            Messenger.Default.Register<ModalMessage<SettingsViewModel>>(
                this,
                msg =>
                {
                    var dialog = new SettingsView();
                    var result = dialog.ShowDialog();
                    msg.Callback(result ?? false, msg.ViewModel);
                });

            Messenger.Default.Register<ModalMessage<WorkItemSelectorViewModel>>(
                this,
                msg =>
                {
                    var dialog = new WorkItemSelectorView(msg.ViewModel);
                    var result = dialog.ShowDialog();
                    msg.Callback(result ?? false, msg.ViewModel);
                });
        }

        private void WorkItemSelected(bool result, WorkItemSelectorViewModel vm)
        {
            if (!result) return;

        }


        #region Events

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #endregion
    }
}