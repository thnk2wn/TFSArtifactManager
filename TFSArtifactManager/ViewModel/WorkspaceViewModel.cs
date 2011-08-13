using System;
using GalaSoft.MvvmLight.Command;

namespace TFSArtifactManager.ViewModel
{
    public class WorkspaceViewModel : AppViewModelBase
    {
        #region RequestClose [event]

        /// <summary>
        /// Raised when this workspace should be removed from the UI.
        /// </summary>
        public event EventHandler RequestClose;

        protected virtual void OnRequestClose()
        {
            var handler = this.RequestClose;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        #endregion // RequestClose [event]

        public virtual string WorkspaceHeader 
        {
            get { return string.Empty; }
        }

        private RelayCommand _closeCommand;
        public RelayCommand CloseCommand
        {
            get { return this._closeCommand ?? (this._closeCommand = new RelayCommand(this.OnRequestClose)); }
        }
    }
}
