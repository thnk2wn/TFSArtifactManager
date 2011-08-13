
using TFSArtifactManager.DI;

namespace TFSArtifactManager.ViewModel
{
    internal class ViewModelLocator
    {
        public MainViewModel Main
        {
            get { return IoC.Get<MainViewModel>(); }
        }

        public ProjectArtifactsViewModel ProjectArtifacts
        {
            get { return IoC.Get<ProjectArtifactsViewModel>(); }
        }

        public SettingsViewModel Settings
        {
            get { return IoC.Get<SettingsViewModel>(); }
        }

        public WorkItemSelectorViewModel WorkItemSelector
        {
            get { return IoC.Get<WorkItemSelectorViewModel>(); }
        }

        public DatabasePackagerViewModel DatabasePackager
        {
            get { return IoC.Get<DatabasePackagerViewModel>(); }
        }
    }
}