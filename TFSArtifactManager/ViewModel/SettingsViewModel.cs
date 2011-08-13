using TFSArtifactManager.Properties;

namespace TFSArtifactManager.ViewModel
{
    internal class SettingsViewModel : AppViewModelBase
    {
        public SettingsViewModel()
        {
            //this.TfsServer = Settings.Default.TfsServerName;
            return;
        }

        public string TfsServer
        {
            get { return Settings.Default.TfsServerName; }
            set
            {
                if (Settings.Default.TfsServerName != value)
                {
                    Settings.Default.TfsServerName = value;
                    Settings.Default.Save();
                    RaisePropertyChanged(()=> TfsServer);
                }
            }
        }

        public int TfsWebUrlPort
        {
            get { return Settings.Default.TfsWebUrlPort; }
            set
            {
                if (Settings.Default.TfsWebUrlPort != value)
                {
                    Settings.Default.TfsWebUrlPort = value;
                    Settings.Default.Save();
                    RaisePropertyChanged(() => TfsWebUrlPort);
                }
            }
        }
    }
}
