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

        internal string OpenObjectQuote { get; private set; } = "[";
        internal string CloseObjectQuote { get; private set; } = "]";

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

        public void SetObjectQuoteStyle(ObjectQuoteStyle style)
        {
            switch (style)
            {
                case ObjectQuoteStyle.DoubleQuote:
                    SetObjectQuoteStyle("\"", "\"");
                    break;

                case ObjectQuoteStyle.BackTick:
                    SetObjectQuoteStyle("`", "`");
                    break;

                case ObjectQuoteStyle.Brackets:
                    SetObjectQuoteStyle("[", "]");
                    break;

                default:
                    throw new ArgumentException($"{style} is not a valid value for a ObjectQuoteStyle", "style");
            }
        }

        public void SetObjectQuoteStyle(string openQuote, string closeQuote)
        {
            OpenObjectQuote = openQuote;
            CloseObjectQuote = closeQuote;
        }

        private class Disposer : IDisposable
        {
            private readonly GlobalSettings toRestore;

            public Disposer() { toRestore = instance; }
            public void Dispose() => instance = toRestore;
        }
    }
}
