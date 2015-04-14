using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CodeOnlyStoredProcedure
{
    internal class GlobalSettings
    {
        private static GlobalSettings instance = new GlobalSettings();

        public static GlobalSettings                   Instance         { get { return instance; } }
        public        IList<IDataTransformer>          DataTransformers { get; private set; }
        public        ConcurrentDictionary<Type, Type> InterfaceMap     { get; private set; }

        private GlobalSettings()
        {
            DataTransformers = new List<IDataTransformer>();
            InterfaceMap     = new ConcurrentDictionary<Type, Type>();
        }

        public static IDisposable UseTestInstance()
        {
            var disposer = new Disposer();
            instance = new GlobalSettings();
            return disposer;
        }

        private class Disposer : IDisposable
        {
            private readonly GlobalSettings toRestore;

            public Disposer()
            {
                toRestore = instance;
            }

            public void Dispose()
            {
                instance = toRestore;
            }
        }
    }
}
