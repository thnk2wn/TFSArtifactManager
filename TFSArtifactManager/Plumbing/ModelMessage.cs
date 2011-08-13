using System;
using GalaSoft.MvvmLight.Messaging;

namespace TFSArtifactManager.Plumbing
{
    public class ModalMessage<TViewModel> : MessageBase
    {
        /// <summary>
        /// The ViewModel that supports the modal dialog.
        /// </summary>
        public TViewModel ViewModel { get; private set; }

        /// <summary>
        /// The callback to be performed when the dialog is dismissed.
        /// </summary>
        public Action<bool, TViewModel> Callback { get; private set; }

        public ModalMessage(Action<bool, TViewModel> callback)
        {
            Callback = callback;
        }

        public ModalMessage(TViewModel vm, Action<bool, TViewModel> callback)
        {
            ViewModel = vm;
            Callback = callback;
        }
    }
}
