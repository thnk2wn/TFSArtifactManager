using System;
using System.IO;
using Ninject.Modules;
using TFSArtifactManager.Plumbing;
using TFSArtifactManager.ViewModel;
using TFSWorkItemChangesetInfo.IO;

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

            Bind<KnownFileTypes>().ToMethod(
                x =>
                    {
                        var configPath = AppDomain.CurrentDomain.BaseDirectory;
                        var known = new KnownFileTypes();
                        known.DatabaseFileTypes.Load(Path.Combine(configPath, "DatabaseFileTypes.xml"));
                        known.ReportFileTypes.Load(Path.Combine(configPath, "ReportFileTypes.xml"));
                        return known;
                    });
        }
    }
}
