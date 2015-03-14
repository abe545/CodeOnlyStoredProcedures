using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure.RowFactory;

namespace CodeOnlyStoredProcedure
{
    internal abstract class RowFactory<T> : IRowFactory<T>
    {
        private Func<IDataReader, T> parser;

        public static IRowFactory<T> Create()
        {
            var itemType = typeof(T);
            if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(Nullable<>))
                itemType = itemType.GetGenericArguments()[0];

            if (itemType.IsEnum)
                return new EnumRowFactory();
            if (itemType.IsSimpleType())
                return new SimpleTypeRowFactory();
            if (itemType == typeof(object))
                return new ExpandoObjectRowFactory();
            
            return new ComplexTypeRowFactory();
        }

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
            while (await reader.ReadAsync(token))
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

        private class SimpleTypeRowFactory : RowFactory<T>
        {
            static readonly ParameterExpression dataReaderExpression = Expression.Parameter(typeof(IDataReader));
            static readonly ValueAccessorFactory<T> accessor = 
                new ValueAccessorFactory<T>(dataReaderExpression, Expression.Constant(0), null, null);

            protected override Func<IDataReader, T> CreateRowFactory(IDataReader reader, IEnumerable<IDataTransformer> xFormers)
            {
                return Expression.Lambda<Func<IDataReader, T>>(accessor.CreateExpressionToGetValueFromReader(reader, xFormers, reader.GetFieldType(0)),
                                                               dataReaderExpression)
                                 .Compile();
            }
        }

        private class EnumRowFactory : RowFactory<T>
        {
            static readonly ParameterExpression dataReaderExpression = Expression.Parameter(typeof(IDataReader));
            static readonly EnumAccessorFactory<T> accessor =
                new EnumAccessorFactory<T>(dataReaderExpression, Expression.Constant(0), null, null);

            protected override Func<IDataReader, T> CreateRowFactory(IDataReader reader, IEnumerable<IDataTransformer> xFormers)
            {
                return Expression.Lambda<Func<IDataReader, T>>(accessor.CreateExpressionToGetValueFromReader(reader, xFormers, reader.GetFieldType(0)),
                                                               dataReaderExpression)
                                 .Compile();
            }
        }

        private class ComplexTypeRowFactory : RowFactory<T>
        {
            private static readonly Type                                                          resultType           = typeof(T);
            private static readonly ParameterExpression                                           dataReaderExpression = Expression.Parameter(typeof(IDataReader));
            private static readonly ParameterExpression                                           indexExpression      = Expression.Variable (typeof(int));
            private static readonly IEnumerable<Tuple<string, PropertyInfo, AccessorFactoryBase>> accessorsByColumnName;
            private static readonly Type                                                          implType;

            static ComplexTypeRowFactory()
            {
                if (!TypeExtensions.interfaceMap.TryGetValue(resultType, out implType))
                    implType = resultType;

                accessorsByColumnName = implType.GetResultPropertiesBySqlName()
                                                .Select(kv =>
                                                {
                                                    var type = kv.Value.PropertyType;
                                                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                                                        type = type.GetGenericArguments().Single();

                                                    if (type.IsEnum)
                                                        return Tuple.Create(kv.Key, kv.Value, (AccessorFactoryBase)Activator.CreateInstance(typeof(EnumAccessorFactory<>).MakeGenericType(kv.Value.PropertyType), dataReaderExpression, indexExpression, kv.Value, kv.Key));

                                                    return Tuple.Create(kv.Key, kv.Value, (AccessorFactoryBase)Activator.CreateInstance(typeof(ValueAccessorFactory<>).MakeGenericType(kv.Value.PropertyType), dataReaderExpression, indexExpression, kv.Value, kv.Key));
                                                })
                                                .ToArray();
            }

            protected override Func<IDataReader, T> CreateRowFactory(IDataReader reader, IEnumerable<IDataTransformer> xFormers)
            {
                var exprs   = new List<Expression>();
                var row     = Expression.Variable(implType);
                var byIndex = accessorsByColumnName.Select(t => Tuple.Create(reader.GetOrdinal(t.Item1), t.Item1, t.Item2, t.Item3))
                                                   .OrderBy(t => t.Item1)
                                                   .ToArray();

                var notFound = byIndex.Where(t => t.Item1 < 0 && !t.Item3.GetCustomAttributes(false).OfType<OptionalResultAttribute>().Any());
                if (notFound.Any())
                    throw new StoredProcedureResultsException(resultType, notFound.Select(t => t.Item2).ToArray());

                exprs.Add(Expression.Assign(row, Expression.New(implType)));

                foreach (var t in byIndex)
                {
                    if (t.Item1 < 0) continue;

                    exprs.Add(Expression.Assign(indexExpression, Expression.Constant(t.Item1)));
                    exprs.Add(Expression.Assign(Expression.Property(row, t.Item3),
                                                t.Item4.CreateExpressionToGetValueFromReader(reader, xFormers, reader.GetFieldType(t.Item1))));
                }

                if (implType == resultType)
                    exprs.Add(row);
                else
                    exprs.Add(Expression.Convert(row, resultType));

                return Expression.Lambda<Func<IDataReader, T>>(
                    Expression.Block(resultType, new[] { row, indexExpression }, exprs),
                    dataReaderExpression).Compile();
            }
        }

        class ExpandoObjectRowFactory : RowFactory<T>
        {
            protected override Func<IDataReader, T> CreateRowFactory(IDataReader reader, IEnumerable<IDataTransformer> xFormers)
            {
                var all = new List<Expression>();
                var rdr = Expression.Parameter(typeof(IDataReader));
                var exp = Expression.Variable(typeof(ExpandoObject));
                var add = typeof(IDictionary<string, object>).GetMethod("Add");
                var val = Expression.Variable(typeof(object[]));

                all.Add(Expression.Assign(exp, Expression.New(typeof(ExpandoObject))));
                all.Add(Expression.Assign(val, Expression.NewArrayBounds(typeof(object), Expression.Constant(reader.FieldCount))));
                all.Add(Expression.Call(rdr, typeof(IDataRecord).GetMethod("GetValues"), val));

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var v = Expression.ArrayIndex(val, Expression.Constant(i));
                    all.Add(Expression.Call(exp, add, Expression.Constant(reader.GetName(i)), v));
                }

                all.Add(Expression.Convert(exp, typeof(T)));

                return Expression.Lambda<Func<IDataReader, T>>(Expression.Block(typeof(T), new[] { exp, val }, all.ToArray()), rdr)
                                 .Compile();
            }
        }
    }
}
