using System.Windows;

namespace TFSArtifactManager.Plumbing
{
    public class MessageBoxService : IMessageBoxService
    {
        #region IMessageBoxService Members

        public void ShowOKDispatch(string text, string header)
        {
            DispatchUtil.SafeDispatch(() => MessageBox.Show(text, header, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        public bool ShowOkCancel(string text, string header)
        {
            return MessageBox.Show(text, header, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK;
        }
        #endregion
    }
}
