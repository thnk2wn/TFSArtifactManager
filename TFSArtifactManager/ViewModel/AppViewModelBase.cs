using GalaSoft.MvvmLight;

namespace TFSArtifactManager.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm/getstarted
    /// </para>
    /// </summary>
    public abstract class AppViewModelBase : ViewModelBase
    {
        private bool _isBusy;

        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    RaisePropertyChanged(() => IsBusy);
                }
            }
        }

        private string _busyText;

        public string BusyText
        {
            get { return _busyText; }
            set
            {
                if (_busyText != value)
                {
                    _busyText = value;
                    RaisePropertyChanged(() => BusyText);
                }
            }
        }
    }
}
