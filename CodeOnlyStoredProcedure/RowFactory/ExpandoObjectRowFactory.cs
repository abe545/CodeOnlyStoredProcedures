using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq.Expressions;

namespace CodeOnlyStoredProcedure.RowFactory
{
    internal class ExpandoObjectRowFactory<T> : RowFactoryBase<T>
    {
        static ExpandoObjectRowFactory()
        {
            if (typeof(T) != typeof(object))
                throw new NotSupportedException("T must be of type object, since that is how the framework passes dynamic.");
        }

        protected override Func<IDataReader, T> CreateRowFactory(IDataReader reader, IEnumerable<IDataTransformer> xFormers)
        {
            var all = new List<Expression>();
            var rdr = Expression.Parameter(typeof(IDataReader));
            var exp = Expression.Variable(typeof(ExpandoObject));
            var add = typeof(IDictionary<string, object>).GetMethod("Add");
            var val = Expression.Variable(typeof(object[]));

            all.Add(Expression.Assign(exp, Expression.New(typeof(ExpandoObject))));
            all.Add(Expression.Assign(val, Expression.NewArrayBounds(typeof(object), Expression.Constant(reader.FieldCount))));
            all.Add(Expression.Call  (rdr, typeof(IDataRecord).GetMethod("GetValues"), val));

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
