using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure.DataTransformation;

namespace CodeOnlyStoredProcedure.RowFactory
{
    internal static class DataTransformerCache
    {
        static readonly Lazy<MethodInfo> canTransform       = new Lazy<MethodInfo>(() => typeof(IDataTransformer)            .GetMethod("CanTransform"));
        static readonly Lazy<MethodInfo> transform          = new Lazy<MethodInfo>(() => typeof(IDataTransformer)            .GetMethod("Transform"));
        static readonly Lazy<MethodInfo> attributeTransform = new Lazy<MethodInfo>(() => typeof(DataTransformerAttributeBase).GetMethod("Transform"));

        public static MethodInfo CanTransform       { get { return canTransform      .Value; } }
        public static MethodInfo Transform          { get { return transform         .Value; } }
        public static MethodInfo AttributeTransform { get { return attributeTransform.Value; } }
    }

    internal static class DataTransformerCache<T>
    {
        static readonly Lazy<MethodInfo> transform          = new Lazy<MethodInfo>(() => typeof(IDataTransformer<T>)         .GetMethod("Transform"));
        static readonly Lazy<MethodInfo> attributeTransform = new Lazy<MethodInfo>(() => typeof(IDataTransformerAttribute<T>).GetMethod("Transform"));

        public static MethodInfo Transform          { get { return transform         .Value; } }
        public static MethodInfo AttributeTransform { get { return attributeTransform.Value; } }
    }
}
