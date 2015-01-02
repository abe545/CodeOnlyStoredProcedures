using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CodeOnlyStoredProcedure.RowFactory
{
    internal abstract class RowFactoryBase<T> : IRowFactory<T>
    {
        private static Lazy<MethodInfo> isDbNull     = new Lazy<MethodInfo>(() => typeof(IDataRecord).GetMethod("IsDBNull"));
        private static Lazy<MethodInfo> getValue     = new Lazy<MethodInfo>(() => typeof(IDataRecord).GetMethod("GetValue"));
        private static Lazy<MethodInfo> canTransform = new Lazy<MethodInfo>(() => typeof(IDataTransformer).GetMethod("CanTransform"));
        private static Lazy<MethodInfo> transform    = new Lazy<MethodInfo>(() => typeof(IDataTransformer).GetMethod("Transform"));
        private Func<IDataReader, T> parser;

        protected static MethodInfo IsDbNullMethod { get { return isDbNull    .Value; } }
        protected static MethodInfo GetValueMethod { get { return getValue    .Value; } }
        protected static MethodInfo CanTransform   { get { return canTransform.Value; } }
        protected static MethodInfo Transform      { get { return transform   .Value; } }

        protected abstract Func<IDataReader, T> CreateRowFactory(IDataReader reader, IEnumerable<IDataTransformer> xFormers);

        public virtual IEnumerable<T> ParseRows(IDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (parser == null)
                parser = CreateRowFactory(reader, dataTransformers);

            var res = new List<T>();
            while (reader.Read())
            {
                token.ThrowIfCancellationRequested();
                res.Add(parser(reader));
            }

            return res;
        }

#if !NET40
        public virtual async Task<IEnumerable<T>> ParseRowsAsync(DbDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token)
        {
            if (parser == null)
                parser = CreateRowFactory(reader, dataTransformers);

            var res = new List<T>();
            while (await reader.ReadAsync())
            {
                token.ThrowIfCancellationRequested();
                res.Add(parser(reader));
            }

            return res;
        }
#endif

        IEnumerable IRowFactory.ParseRows(IDataReader reader, IEnumerable<IDataTransformer> transformers, CancellationToken token)
        {
            return ParseRows(reader, transformers, token);
        }
    }
}
