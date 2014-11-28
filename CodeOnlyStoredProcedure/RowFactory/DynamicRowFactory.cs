using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CodeOnlyStoredProcedure
{
    internal class DynamicRowFactory<T> : IRowFactory
        where T : new()
    {
        private static   IDictionary<string, Action<T, object, IEnumerable<IDataTransformer>>> setters = new Dictionary<string, Action<T, object, IEnumerable<IDataTransformer>>>();
        private readonly HashSet<string>                                                       unfoundProps;

        public IEnumerable<string> UnfoundPropertyNames { get { return unfoundProps; } }

        static DynamicRowFactory()
        {
            var tType         = typeof(T);
            var listType      = typeof(IEnumerable<IDataTransformer>);
            var row           = Expression.Parameter(tType,          "row");
            var val           = Expression.Parameter(typeof(object), "value");
            var gts           = Expression.Parameter(listType,       "globalTransformers");
            var xf            = typeof(DataTransformerAttributeBase).GetMethod("Transform");
            var props         = tType.GetResultPropertiesBySqlName();
            var propertyAttrs = props.ToDictionary(kv => kv.Key,
                                                    kv => kv.Value
                                                            .GetCustomAttributes(false)
                                                            .Cast<Attribute>()
                                                            .ToArray()
                                                            .AsEnumerable());
                
            foreach (var kv in props)
            {
                var currentType = kv.Value.PropertyType;
                var expressions = new List<Expression>();
                var iterType    = typeof(IEnumerator<IDataTransformer>);
                var iter        = Expression.Variable(iterType, "iter");
                var propType    = Expression.Constant(currentType, typeof(Type));             
                var isNullable  = Expression.Constant(currentType.IsGenericType &&
                                                        currentType.GetGenericTypeDefinition() == typeof(Nullable<>));

                if ((bool)isNullable.Value)
                    propType = Expression.Constant(kv.Value.PropertyType.GetGenericArguments().Single());

                IEnumerable<Attribute> attrs;
                if (propertyAttrs.TryGetValue(kv.Key, out attrs))
                {
                    var propTransformers = attrs.OfType<DataTransformerAttributeBase>().OrderBy(a => a.Order);           
                    var xformType        = typeof(IDataTransformer);
                    var attrsExpr        = Expression.Constant(attrs, typeof(IEnumerable<Attribute>));
                    var transformer      = Expression.Variable(xformType, "transformer");
                    var endFor           = Expression.Label("endForEach");

                    var doTransform = Expression.IfThen(
                        Expression.Call(transformer, xformType.GetMethod("CanTransform"), val, propType, isNullable, attrsExpr),
                        Expression.Assign(val,
                            Expression.Call(transformer, xformType.GetMethod("Transform"), val, propType, isNullable, attrsExpr)));

                    expressions.Add(Expression.Assign(iter, Expression.Call(gts, listType.GetMethod("GetEnumerator"))));
                    var loopBody = Expression.Block(
                        new[] { transformer },
                        Expression.Assign(transformer, Expression.Property(iter, iterType.GetProperty("Current"))),
                        doTransform);

                    expressions.Add(Expression.Loop(
                        Expression.IfThenElse(Expression.Call(iter, typeof(IEnumerator).GetMethod("MoveNext")),
                                                loopBody,
                                                Expression.Break(endFor)),
                        endFor));
                                                
                    if (kv.Value.PropertyType.IsEnum)
                    {
                        // nulls are always false for a TypeIs operation, so no need to explicitly check for it
                        expressions.Add(Expression.IfThen(Expression.TypeIs(val, typeof(string)),
                                                            Expression.Assign(val, Expression.Call(typeof(Enum).GetMethod("Parse", new[] { typeof(Type), typeof(string) }),
                                                                                                    propType,
                                                                                                    Expression.Convert(val, typeof(string))))));
                    }
                        
                    foreach (var xform in propTransformers)
                        expressions.Add(Expression.Assign(val, Expression.Call(Expression.Constant(xform), xf, val, propType, isNullable)));
                }

                Expression conv;
                if (kv.Value.PropertyType.IsValueType)
                    conv = Expression.Unbox(val, kv.Value.PropertyType);
                else
                    conv = Expression.Convert(val, kv.Value.PropertyType);

                expressions.Add(Expression.Assign(Expression.Property(row, kv.Value), conv));

                var body   = Expression.Block(new[] { iter }, expressions.ToArray());
                var lambda = Expression.Lambda<Action<T, object, IEnumerable<IDataTransformer>>>(body, row, val, gts);

                setters.Add(kv.Key, lambda.Compile());
            }
        }

        public DynamicRowFactory()
        {
            unfoundProps = new HashSet<string>(typeof(T).GetRequiredPropertyNames());
        }

        public object CreateRow(
            string[]                      fieldNames, 
            object[]                      values,
            IEnumerable<IDataTransformer> transformers)
        {
            var row = new T();
            Action<T, object, IEnumerable<IDataTransformer>> set;

            for (int i = 0; i < values.Length; i++)
            {
                var name = fieldNames[i];
                if (setters.TryGetValue(name, out set))
                {
                    var value = values[i];
                    if (DBNull.Value.Equals(value))
                        value = null;

                    try
                    {
                        set(row, value, transformers);
                    }
                    catch(Exception ex)
                    {
                        var prop = typeof(T).GetResultPropertiesBySqlName()[name];
                        throw new StoredProcedureColumnException(prop.Name, prop.PropertyType, value, ex);
                    }
                    unfoundProps.Remove(name);
                }
            }

            return row;
        }
    }
}
