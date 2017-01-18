using System;
using System.ComponentModel.Composition;
using System.Data;

namespace SmokeTests
{
    public interface ISmokeTest
    {
        string Name { get; }
        bool Ignore { get; }
    }

    [MetadataAttribute, AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SmokeTestAttribute : ExportAttribute, ISmokeTest
    {
        public string Name { get; }
        public bool Ignore { get; }

        public SmokeTestAttribute(string name, bool ignore = false)
        {
            Name = name;
            Ignore = ignore;
        }
    }
}
