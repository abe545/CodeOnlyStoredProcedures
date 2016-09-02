using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CodeOnlyStoredProcedure
{
    internal class GlobalSettings
    {
        private static GlobalSettings instance = new GlobalSettings();

        public static GlobalSettings                   Instance                { get { return instance; } }
        public        IList<IDataTransformer>          DataTransformers        { get; } = new List<IDataTransformer>();
        public        ConcurrentDictionary<Type, Type> InterfaceMap            { get; } = new ConcurrentDictionary<Type, Type>();
        public        bool                             IsTestInstance          { get; }
        public        bool                             ConvertAllNumericValues { get; set; }
        public        bool                             CloneConnectionForEachCall { get; set; } = true;

        private GlobalSettings(bool isTestInstance = false)
        {
            IsTestInstance = isTestInstance;
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
