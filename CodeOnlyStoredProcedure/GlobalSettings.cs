using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CodeOnlyStoredProcedure
{
    internal class GlobalSettings
    {
        private static GlobalSettings instance = new GlobalSettings();

        public static GlobalSettings                   Instance                { get { return instance; } }
        public        IList<IDataTransformer>          DataTransformers        { get; }
        public        ConcurrentDictionary<Type, Type> InterfaceMap            { get; }
        public        bool                             IsTestInstance          { get; }
        public        bool                             ConvertAllNumericValues { get; set; }

        private GlobalSettings(bool isTestInstance = false)
        {
            IsTestInstance   = isTestInstance;
            DataTransformers = new List<IDataTransformer>();
            InterfaceMap     = new ConcurrentDictionary<Type, Type>();
        }

        public static IDisposable UseTestInstance()
        {
            var disposer = new Disposer();
            instance = new GlobalSettings(true);
            return disposer;
        }

        private class Disposer : IDisposable
        {
            private readonly GlobalSettings toRestore;

            public Disposer() { toRestore = instance; }
            public void Dispose() => instance = toRestore;
        }
    }
}
