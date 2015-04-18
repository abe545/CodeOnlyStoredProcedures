using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeOnlyStoredProcedure.RowFactory
{
    internal class ComplexTypeRowFactory<T> : RowFactory<T>
    {
        private static readonly Type                resultType           = typeof(T);
        private static readonly ParameterExpression indexExpression      = Expression.Variable (typeof(int));
        private static readonly IEnumerable<Column> accessorsByColumnName;
        private static readonly Type                implType;
        private static readonly IEnumerable<string> dbColumnNames;
        private static readonly IEnumerable<string> requiredColumnNames;

        static ComplexTypeRowFactory()
        {
            if (!GlobalSettings.Instance.InterfaceMap.TryGetValue(resultType, out implType))
                implType = resultType;

            var props             = implType.GetResultPropertiesBySqlName();
            dbColumnNames         = props.Keys.ToArray();
            requiredColumnNames   = props.Where(kv => !kv.Value.GetCustomAttributes(false).OfType<OptionalResultAttribute>().Any())
                                         .Where(kv => !kv.Value.PropertyType.IsEnumeratedType()) // child collections can't be required
                                         .Select(kv => kv.Key)
                                         .ToArray();
            accessorsByColumnName = props.Where(kv => !kv.Value.PropertyType.IsEnumeratedType())
                                         .Select(kv =>
                                         {
                                             var type = kv.Value.PropertyType;
                                             if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                                                 type = type.GetGenericArguments().Single();

                                             if (type.IsEnum)
                                             {
                                                 return new Column
                                                 {
                                                     Name            = kv.Key,
                                                     Property        = kv.Value,
                                                     IsOptional      = kv.Value.GetCustomAttributes(false).OfType<OptionalResultAttribute>().Any(),
                                                     AccessorFactory = (AccessorFactoryBase)Activator.CreateInstance(typeof(EnumAccessorFactory<>).MakeGenericType(kv.Value.PropertyType), dataReaderExpression, indexExpression, kv.Value, kv.Key)
                                                 };
                                             }

                                             return new Column
                                             {
                                                 Name            = kv.Key,
                                                 Property        = kv.Value,
                                                 IsOptional      = kv.Value.GetCustomAttributes(false).OfType<OptionalResultAttribute>().Any(),
                                                 AccessorFactory = (AccessorFactoryBase)Activator.CreateInstance(typeof(ValueAccessorFactory<>).MakeGenericType(kv.Value.PropertyType), dataReaderExpression, indexExpression, kv.Value, kv.Key)
                                             };
                                         })
                                         .ToArray();
        }

        protected override Func<IDataReader, T> CreateRowFactory(IDataReader reader, IEnumerable<IDataTransformer> xFormers)
        {
            var exprs   = new List<Expression>();
            var row     = Expression.Variable(implType);
            var byIndex = accessorsByColumnName.Select(c => new { Index = reader.GetOrdinal(c.Name), c.Name, c.AccessorFactory, c.Property, c.IsOptional })
                                               .OrderBy(c => c.Index)
                                               .ToArray();

            var notFound = byIndex.Where(c => c.Index < 0 && !c.IsOptional);
            if (notFound.Any())
                throw new StoredProcedureResultsException(resultType, notFound.Select(t => t.Name).ToArray());

            exprs.Add(Expression.Assign(row, Expression.New(implType)));

            foreach (var c in byIndex)
            {
                if (c.Index < 0) continue;

                exprs.Add(Expression.Assign(indexExpression, Expression.Constant(c.Index)));
                exprs.Add(Expression.Assign(Expression.Property(row, c.Property),
                                            c.AccessorFactory.CreateExpressionToGetValueFromReader(reader, xFormers, reader.GetFieldType(c.Index))));
            }

            if (implType == resultType)
                exprs.Add(row);
            else
                exprs.Add(Expression.Convert(row, resultType));

            return Expression.Lambda<Func<IDataReader, T>>(
                Expression.Block(resultType, new[] { row, indexExpression }, exprs),
                dataReaderExpression).Compile();
        }

        public override bool MatchesColumns(IEnumerable<string> columnNames, out int leftoverColumns)
        {
            leftoverColumns = columnNames.Except(dbColumnNames).Count();
            return requiredColumnNames.All(columnNames.Contains);
        }

        private class Column
        {
            public string              Name            { get; set; }
            public bool                IsOptional      { get; set; }
            public PropertyInfo        Property        { get; set; }
            public AccessorFactoryBase AccessorFactory { get; set; }
        }
    }
}
