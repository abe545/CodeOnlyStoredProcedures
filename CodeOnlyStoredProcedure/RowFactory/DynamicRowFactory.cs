//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Reflection.Emit;
//using System.Threading.Tasks;

//namespace CodeOnlyStoredProcedure
//{
//    internal class DynamicRowFactory<T> : IRowFactory<T>
//        where T : new()
//    {
//        private delegate void Set(T row, object value, IEnumerable<IDataTransformer> transformers, Attribute[] attrs);

//        private static   IDictionary<string, Tuple<Set, Attribute[]>> setters = new Dictionary<string, Tuple<Set, Attribute[]>>();
//        private readonly HashSet<string>                              unfoundProps;

//        public IEnumerable<string> UnfoundPropertyNames { get { return unfoundProps; } }

//        static DynamicRowFactory()
//        {
//            var tType         = typeof(T);
//            var props         = tType.GetResultPropertiesBySqlName();
//            var propertyAttrs = props.ToDictionary(kv => kv.Key,
//                                                    kv => kv.Value
//                                                            .GetCustomAttributes(false)
//                                                            .Cast<Attribute>()
//                                                            .ToArray()
//                                                            .AsEnumerable());
            
//            foreach (var kv in props)
//            {
//                var method = new DynamicMethod(
//                    "Set_" + kv.Key,
//                    typeof(void),
//                    new[] { tType, typeof(object), typeof(IEnumerable<IDataTransformer>), typeof(Attribute[]) },
//                    restrictedSkipVisibility: true);
                
//                var il          = method.GetILGenerator();
//                var currentType = kv.Value.PropertyType;
//                var iterType    = typeof(IEnumerator<IDataTransformer>);
//                var isNullable  = currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(Nullable<>);

//                if (isNullable)
//                    currentType = currentType.GetGenericArguments().Single();

//                var propAttrs = new Attribute[0];

//                method.DefineParameter(0, ParameterAttributes.None, "t");
//                method.DefineParameter(1, ParameterAttributes.None, "value");
//                method.DefineParameter(2, ParameterAttributes.None, "transformers");
//                method.DefineParameter(3, ParameterAttributes.None, "attributes");

//                IEnumerable<Attribute> attrs;
//                if (propertyAttrs.TryGetValue(kv.Key, out attrs))
//                {
//                    propAttrs = attrs.OrderBy(a => a is DataTransformerAttributeBase ? ((DataTransformerAttributeBase)a).Order : Int32.MaxValue)
//                                     .ToArray();
//                    var hasPropTransformAttr = propAttrs.OfType<DataTransformerAttributeBase>().Any();

//                    il.DeclareLocal(typeof(Type));
//                    il.DeclareLocal(typeof(IDataTransformer));
//                    il.DeclareLocal(typeof(IEnumerator<IDataTransformer>));

//                    if (hasPropTransformAttr)
//                        il.DeclareLocal(typeof(int));

//                    // store the type that will be passed to all the transformers
//                    il.Emit(OpCodes.Ldtoken, currentType);
//                    il.EmitCall(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"), null);
//                    il.Emit(OpCodes.Stloc_0);

//                    // get the iterator from the 2nd argument (IEnumerable<IDataTransformer>)
//                    il.Emit(OpCodes.Ldarg_2);
//                    il.EmitCall(OpCodes.Callvirt, typeof(IEnumerable<IDataTransformer>).GetMethod("GetEnumerator"), null);
//                    il.Emit(OpCodes.Stloc_2);   // assign the IEnumerator to our variable

//                    // foreach
//                    var forEachBlock = il.BeginExceptionBlock();
//                    var startLoop    = il.DefineLabel();
//                    var moveNext     = il.DefineLabel();
//                    il.Emit(OpCodes.Br_S, moveNext);
                    
//                    il.MarkLabel(startLoop);
//                    il.Emit(OpCodes.Ldloc_2);   // load the IEnumerator, and get the current value
//                    il.EmitCall(OpCodes.Callvirt, typeof(IEnumerator<IDataTransformer>).GetProperty("Current").GetGetMethod(), null);
//                    il.Emit(OpCodes.Stloc_1);   // store it in the local variable
                    
//                    // check to see if the transformer can perform the transformation
//                    il.Emit(OpCodes.Ldloc_1);   
//                    il.Emit(OpCodes.Ldarg_1);
//                    il.Emit(OpCodes.Ldloc_0);
//                    il.Emit(isNullable ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
//                    il.Emit(OpCodes.Ldarg_3);
//                    il.EmitCall(OpCodes.Callvirt, typeof(IDataTransformer).GetMethod("CanTransform"), null);
//                    il.Emit(OpCodes.Brfalse_S, moveNext);  // can't transform, continue loop

//                    // perform the transformation
//                    il.Emit(OpCodes.Ldloc_1);
//                    il.Emit(OpCodes.Ldarg_1);
//                    il.Emit(OpCodes.Ldloc_0);
//                    il.Emit(isNullable ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
//                    il.Emit(OpCodes.Ldarg_3);
//                    il.EmitCall(OpCodes.Callvirt, typeof(IDataTransformer).GetMethod("Transform"), null);
//                    il.Emit(OpCodes.Starg_S, 1);

//                    // load the IEnumerator, and call move next
//                    il.MarkLabel(moveNext);
//                    il.Emit(OpCodes.Ldloc_2);   
//                    il.EmitCall(OpCodes.Callvirt, typeof(IEnumerator).GetMethod("MoveNext"), null);
//                    il.Emit(OpCodes.Brtrue_S, startLoop);

//                    // finally block
//                    il.BeginFinallyBlock();
//                    var endFinally = il.DefineLabel();
//                    il.Emit(OpCodes.Ldloc_2);
//                    il.Emit(OpCodes.Brfalse_S, endFinally);
//                    il.Emit(OpCodes.Ldloc_2);
//                    il.EmitCall(OpCodes.Callvirt, typeof(IDisposable).GetMethod("Dispose"), null);
//                    il.MarkLabel(endFinally);
//                    il.EndExceptionBlock();

//                    // if the property is an enum, try to parse strings
//                    if (currentType.IsEnum)
//                    {
//                        // if it isn't a string, abort!
//                        var notAString = il.DefineLabel();
//                        il.Emit(OpCodes.Ldarg_1);
//                        il.Emit(OpCodes.Isinst, typeof(string));
//                        il.Emit(OpCodes.Brfalse_S, notAString);

//                        // parse the string
//                        il.Emit(OpCodes.Ldloc_0);
//                        il.Emit(OpCodes.Ldarg_1);
//                        il.Emit(OpCodes.Castclass, typeof(string));
//                        il.Emit(OpCodes.Ldc_I4_1);  // ignore case
//                        il.EmitCall(OpCodes.Call, typeof(Enum).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(Type), typeof(string), typeof(bool) }, null), null);
//                        il.Emit(OpCodes.Starg_S, 1);

//                        il.MarkLabel(notAString);
//                    }

//                    // run the data transformers decorating the property
//                    if (hasPropTransformAttr)
//                    {
//                        var loopCondition = il.DefineLabel();
//                        var increment     = il.DefineLabel();
//                        startLoop         = il.DefineLabel();

//                        // for loop... i = 0
//                        il.Emit(OpCodes.Ldc_I4_0);
//                        il.Emit(OpCodes.Stloc_3);
//                        il.Emit(OpCodes.Br_S, loopCondition);

//                        // loop body
//                        il.MarkLabel(startLoop);
//                        il.Emit(OpCodes.Ldarg_3);
//                        il.Emit(OpCodes.Ldloc_3);
//                        il.Emit(OpCodes.Ldelem, typeof(Attribute));
//                        il.Emit(OpCodes.Isinst, typeof(DataTransformerAttributeBase));
//                        il.Emit(OpCodes.Brfalse_S, increment);
                        
//                        il.Emit(OpCodes.Ldarg_3);
//                        il.Emit(OpCodes.Ldloc_3);
//                        il.Emit(OpCodes.Ldelem, typeof(Attribute));
//                        il.Emit(OpCodes.Castclass, typeof(DataTransformerAttributeBase));
//                        il.Emit(OpCodes.Ldarg_1);
//                        il.Emit(OpCodes.Ldloc_0);
//                        il.Emit(isNullable ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
//                        il.EmitCall(OpCodes.Callvirt, typeof(DataTransformerAttributeBase).GetMethod("Transform"), null);
//                        il.Emit(OpCodes.Starg_S, 1);

//                        // increment i
//                        il.MarkLabel(increment);
//                        il.Emit(OpCodes.Ldloc_3);
//                        il.Emit(OpCodes.Ldc_I4_1);
//                        il.Emit(OpCodes.Add);
//                        il.Emit(OpCodes.Stloc_3);

//                        // check i < array length
//                        il.MarkLabel(loopCondition);
//                        il.Emit(OpCodes.Ldloc_3);
//                        il.Emit(OpCodes.Ldarg_3);
//                        il.Emit(OpCodes.Ldlen);
//                        il.Emit(OpCodes.Conv_I4);
//                        il.Emit(OpCodes.Blt_S, startLoop);
//                    }
//                }

//                // set the value
//                il.Emit(OpCodes.Ldarg_0);
//                il.Emit(OpCodes.Ldarg_1);

//                // this works for if this is S from Nullable<S>, because only values types can be S
//                if (currentType.IsValueType)
//                    il.Emit(OpCodes.Unbox_Any, kv.Value.PropertyType);
//                else
//                    il.Emit(OpCodes.Castclass, kv.Value.PropertyType);

//                // set the property value
//                il.EmitCall(OpCodes.Callvirt, kv.Value.GetSetMethod(), null);
//                il.Emit(OpCodes.Ret);

//                setters.Add(kv.Key, Tuple.Create((Set)method.CreateDelegate(typeof(Set)), propAttrs));
//            }
//        }

//        public DynamicRowFactory()
//        {
//            unfoundProps = new HashSet<string>(typeof(T).GetRequiredPropertyNames());
//        }

//        public T CreateRow(
//            string[]                      fieldNames, 
//            object[]                      values,
//            IEnumerable<IDataTransformer> transformers)
//        {
//            var row = new T();
//            Tuple<Set, Attribute[]> setter;

//            for (int i = 0; i < values.Length; i++)
//            {
//                var name = fieldNames[i];
//                if (setters.TryGetValue(name, out setter))
//                {
//                    var value = values[i];
//                    if (DBNull.Value.Equals(value))
//                        value = null;

//                    try
//                    {
//                        setter.Item1(row, value, transformers, setter.Item2);
//                    }
//                    catch (Exception ex)
//                    {
//                        var prop = typeof(T).GetResultPropertiesBySqlName()[name];
//                        throw new StoredProcedureColumnException(prop.PropertyType, value != null ? value.GetType() : typeof(void), ex, prop.Name);
//                    }
//                    unfoundProps.Remove(name);
//                }
//            }

//            return row;
//        }

//        public IEnumerable<T> ParseRows(System.Data.IDataReader reader, IEnumerable<IDataTransformer> DataTransformers, System.Threading.CancellationToken token)
//        {
//            return Enumerable.Empty<T>();
//        }

//#if !NET40
//        public System.Threading.Tasks.Task<IEnumerable<T>> ParseRowsAsync(System.Data.Common.DbDataReader reader, IEnumerable<IDataTransformer> DataTransformers, System.Threading.CancellationToken token)
//        {
//            return Task.Run(() => ParseRows(reader, DataTransformers, token), token);
//        }
//#endif
//    }
//}
