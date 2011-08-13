using System;
using System.Collections.Generic;
using Ninject;
using Ninject.Modules;

namespace TFSArtifactManager.DI
{
    internal class IoC
    {
        private static IKernel _kernel;

        private static IKernel Kernel
        {
            get { return _kernel ?? (_kernel = new StandardKernel()); }
        }

        public static void Load(INinjectModule module)
        {
            Kernel.Load(module);
        }

        public static void Load(IEnumerable<INinjectModule> modules)
        {
            Kernel.Load(modules);
        }

        public static T Get<T>()
        {
            return Kernel.Get<T>();
        }

        public static object Get(Type type)
        {
            return Kernel.Get(type);
        }
    }
}
