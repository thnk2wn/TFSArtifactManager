using System.Windows;
using GalaSoft.MvvmLight.Threading;
using TFSArtifactManager.DI;
using Ninject.Modules;

namespace TFSArtifactManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App //: Application
    {
        static App()
        {
            DispatcherHelper.Initialize();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            IoC.Load(new NinjectModule[] { new MainModule() });
        }
    }
}
