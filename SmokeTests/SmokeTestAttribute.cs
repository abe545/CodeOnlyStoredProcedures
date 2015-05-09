using System;
using System.ComponentModel.Composition;
using System.Data;

namespace SmokeTests
{
    public interface ISmokeTest
    {
        string Name { get; }
    }

    [MetadataAttribute, AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SmokeTestAttribute : ExportAttribute, ISmokeTest
    {
        public string Name { get; private set; }

        public SmokeTestAttribute(string name)
        {
            Name = name;
        }
    }
}
