using Ninject.Modules;
using TFSArtifactManager.Plumbing;
using TFSArtifactManager.ViewModel;

namespace TFSArtifactManager.DI 
{
    internal class MainModule : NinjectModule
    {
        public override void Load()
        {
            Bind<MainViewModel>().ToSelf().InSingletonScope();
            
            Bind<ProjectArtifactsViewModel>().ToSelf().InSingletonScope();
            Bind<DatabasePackagerViewModel>().ToSelf().InSingletonScope();

            Bind<WorkItemSelectorViewModel>().ToSelf();

            Bind<IMessageBoxService>().To<MessageBoxService>();
        }
    }
}
