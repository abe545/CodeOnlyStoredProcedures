using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using CodeOnlyStoredProcedure.DataTransformation;

namespace CodeOnlyStoredProcedure.RowFactory
{
    internal sealed class CodeSteppingInfo
    {
        private readonly Dictionary<string, Attribute[]> typedTransformerAttributes = new Dictionary<string, Attribute[]>();
        private readonly Dictionary<string, DataTransformerAttributeBase[]> transformerAttributes = new Dictionary<string, DataTransformerAttributeBase[]>();
        private readonly Dictionary<string, Attribute[]> allAttributes = new Dictionary<string, Attribute[]>();
        private readonly Dictionary<string, IDataTransformer[]> untypedTransformers = new Dictionary<string, IDataTransformer[]>();
        private readonly Dictionary<string, IDataTransformer[]> typedTransformers = new Dictionary<string, IDataTransformer[]>();
        private readonly TypeBuilder typeBuilder;
        private readonly string typeName;
        private int line = 8;
        private int indent = 3;
        private string filename;

        public string               Name               { get; private set; }
        public StringBuilder        SourceCode         { get; private set; }
        public DebugInfoGenerator   DebugInfoGenerator { get; private set; }
        public SymbolDocumentInfo   SymbolDocument     { get; private set; }

        public CodeSteppingInfo(Type returnType)
        {
            Contract.Requires(returnType != null);

            var id             = Guid.NewGuid().ToString().Replace('-', '_');
            Name               = "StoredProcedureResultsParser_" + id;
            filename           = Path.Combine(Path.GetTempPath(), Name + ".cs");
            SourceCode         = new StringBuilder();
            DebugInfoGenerator = DebugInfoGenerator.CreatePdbGenerator();
            SymbolDocument     = Expression.SymbolDocument(filename, SymLanguageType.CSharp);

            var assemblyName    = new AssemblyName(Name);
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);

            // Mark generated code as debuggable. 
            // See http://blogs.msdn.com/rmbyers/archive/2005/06/26/432922.aspx for explanation.        
            var daCtor    = typeof(DebuggableAttribute).GetConstructor(new Type[] { typeof(DebuggableAttribute.DebuggingModes) });
            var daBuilder = new CustomAttributeBuilder(daCtor, new object[] { 
                DebuggableAttribute.DebuggingModes.DisableOptimizations | 
                DebuggableAttribute.DebuggingModes.Default });
            assemblyBuilder.SetCustomAttribute(daBuilder);

            var module  = assemblyBuilder.DefineDynamicModule(Name, true);
            typeBuilder = module.DefineType("StoredProcedureResultsParser", TypeAttributes.Public | TypeAttributes.Class);

            var namespaces = (new [] { "System", "System.Data", "CodeOnlyStoredProcedure", returnType.Namespace }).Distinct();
            var usings = namespaces.Where(ns => ns.StartsWith("System"))
                                   .OrderBy(ns => ns)
                                   .Concat(namespaces.Where(ns => !ns.StartsWith("System"))
                                                     .OrderBy(ns => ns))
                                   .Select(s => string.Format("using {0};", s));

            foreach (var ns in usings)
            {
                SourceCode.AppendLine(ns);
                ++line;
            }

            typeName = returnType.Name;
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                returnType = returnType.GetGenericArguments().Single();
                typeName = returnType.Name + "?";
            }

            SourceCode.AppendLine()
                      .Append("namespace CodeOnlyStoredProcedure.RowFactory_")
                      .AppendLine(id)
                      .AppendLine("{")
                      .AppendLine("    public static class StoredProcedureResultsParser")
                      .AppendLine("    {")
                      .Append("        public static ")
                      .Append(typeName);
        }

        ~CodeSteppingInfo()
        {
            Dispose(false);
        }

        public Func<IDataReader, T> CompileMethod<T>(Expression body, ParameterExpression argument)
        {
            Contract.Requires(body != null);
            Contract.Requires(argument != null);
            Contract.Ensures(Contract.Result<Func<IDataReader, T>>() != null);

            var builder = typeBuilder.DefineMethod(
                "Parse",
                MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.Public,
                typeof(T),
                new Type[] { typeof(IDataReader) });
            Expression.Lambda<Func<IDataReader, T>>(body, argument)
                        .CompileToMethod(builder, DebugInfoGenerator);

            SourceCode.AppendLine("        }");

            AddFields(typedTransformerAttributes.Keys, string.Format("IDataTransformerAttribute<{0}>", typeName));
            AddFields(transformerAttributes.Keys, "DataTransformerAttributeBase");
            AddFields(untypedTransformers.Keys, "IDataTransformer");
            AddFields(allAttributes.Keys, "Attribute");

            SourceCode.AppendLine()
                      .AppendLine("    }")
                      .AppendLine("}");

            using (var tmp = File.CreateText(filename))
                tmp.Write(SourceCode.ToString());

            var t = typeBuilder.CreateType();

            SetStaticValues(t, typedTransformerAttributes);
            SetStaticValues(t, transformerAttributes);
            SetStaticValues(t, untypedTransformers);
            SetStaticValues(t, allAttributes);

            return (Func<IDataReader, T>)Delegate.CreateDelegate(typeof(Func<IDataReader, T>), t.GetMethod("Parse"));
        }

        public void BeginBlock()
        {
            AppendIndentToSourceCode(false);
            SourceCode.AppendLine("{");
            ++indent;
            ++line;
        }

        public void EndBlock()
        {
            --indent;
            AppendIndentToSourceCode(false);
            SourceCode.AppendLine("}");
            ++line;
        }

        public void StartParseMethod(string argumentName)
        {
            Contract.Requires(!string.IsNullOrEmpty(argumentName));
            SourceCode.Append(" Parse(IDataReader ")
                      .Append(argumentName)
                      .AppendLine(")")
                      .AppendLine("        {");
        }

        public DebugInfoExpression MarkLine(string code, bool indentOneMore = false)
        {
            Contract.Requires(!string.IsNullOrEmpty(code));
            Contract.Ensures(Contract.Result<DebugInfoExpression>() != null);

            AppendIndentToSourceCode(indentOneMore);
            SourceCode.AppendLine(code);
            var start = 1 + indent * 4;
            if (indentOneMore)
                start += 4;

            return Expression.DebugInfo(SymbolDocument, line, start, line++, start + code.Length);
        }

        public IEnumerable<Expression> AddTransformers(
            IDataTransformer[] transformers, 
            MethodInfo canTransform, 
            MethodInfo transform, 
            ParameterExpression value, 
            ConstantExpression isNullable, 
            Type targetType,
            Attribute[] attributes, 
            string propertyName)
        {
            Contract.Requires(transformers != null);
            Contract.Requires(canTransform != null);
            Contract.Requires(transform != null);
            Contract.Requires(value != null);
            Contract.Requires(isNullable != null);
            Contract.Requires(attributes != null);
            Contract.Requires(propertyName != null);
            Contract.Ensures(Contract.Result<IEnumerable<Expression>>() != null);

            allAttributes.Add(propertyName + "Attributes", attributes);
            untypedTransformers.Add(propertyName + "Transformers", transformers);

            var xformerField = DefineField(propertyName + "Transformers", typeof(IDataTransformer[]));
            var attrField    = DefineField(propertyName + "Attributes",   typeof(Attribute[]));

            for (int i = 0; i < transformers.Length; i++)
            {
                yield return MarkLine(string.Format("if ({0}Transformers[{1}].CanTransform({2}, typeof({3}), {4}, {0}Attributes))",
                    propertyName, i, value.Name.ToLower(), typeName, isNullable.Value));
                yield return Expression.IfThen(
                    Expression.Call(Expression.ArrayIndex(Expression.Field(null, xformerField), Expression.Constant(i)),
                        canTransform,
                        value,
                        Expression.Constant(targetType, typeof(Type)),
                        isNullable,
                        Expression.Field(null, attrField)),
                    Expression.Block(
                        MarkLine(string.Format("{2} = {0}Transformers[{1}].Transform({2}, typeof({3}), {4}, {0}Attributes);",
                            propertyName, i, value.Name.ToLower(), typeName, isNullable.Value), true),
                        Expression.Assign(
                            value,
                            Expression.Call(Expression.ArrayIndex(Expression.Field(null, xformerField), Expression.Constant(i)),
                                transform,
                                value,
                                Expression.Constant(targetType, typeof(Type)),
                                isNullable,
                                Expression.Field(null, attrField)
                            )
                        )
                    )
                );
            }
        }

        public IEnumerable<Expression> AddTransformers(
            DataTransformerAttributeBase[] transformers,
            MethodInfo method,
            ParameterExpression retVal,
            Expression target,
            ConstantExpression isNullable,
            string name = "attributeTransformers")
        {
            Contract.Requires(transformers != null);
            Contract.Requires(method != null);
            Contract.Requires(retVal != null);
            Contract.Requires(isNullable != null);
            Contract.Requires(!string.IsNullOrEmpty(name));
            Contract.Ensures(Contract.Result<IEnumerable<Expression>>() != null);

            transformerAttributes.Add(name, transformers);
            var field = DefineField(name, typeof(DataTransformerAttributeBase[]));

            for (int i = 0; i < transformers.Length; i++)
            {
                yield return MarkLine(string.Format("{0} = {1}[{2}].Transform({0}, {3}, {4});", retVal.Name, name, i, typeName, isNullable.Value));
                yield return Expression.Assign(retVal,
                    Expression.Call(Expression.ArrayIndex(Expression.Field(null, field)), method, retVal, target, isNullable));
            }
        }

        public IEnumerable<Expression> AddTransformers<T>(
            IDataTransformerAttribute<T>[] transformers, 
            MethodInfo method, 
            ParameterExpression retVal, 
            string name)
        {
            Contract.Requires(transformers != null);
            Contract.Requires(method != null);
            Contract.Requires(retVal != null);
            Contract.Requires(!string.IsNullOrEmpty(name));
            Contract.Ensures(Contract.Result<IEnumerable<Expression>>() != null);

            typedTransformerAttributes.Add(name, (Attribute[])transformers);
            var field = DefineField(name, typeof(IDataTransformerAttribute<T>[]));
            
            for (int i = 0; i < transformers.Length; i++)
            {
                yield return MarkLine(string.Format("{0} = {1}[{2}].Transform({0});", retVal.Name, name, i));
                yield return Expression.Assign(retVal, 
                    Expression.Call(Expression.ArrayIndex(Expression.Field(null, field)), method, retVal));
            }
        }

        public Expression AddTransformers<T>(
            IDataTransformer<T>[] transformers,
            Attribute[] attributes,
            Expression body,
            string propertyName)
        {
            Contract.Requires(transformers != null);
            Contract.Requires(attributes != null);
            Contract.Requires(body != null);
            Contract.Requires(propertyName != null);
            Contract.Ensures(Contract.Result<Expression>() != null);

            if (transformers.Length == 0)
                return body;

            allAttributes.Add(propertyName + "Attributes", attributes);
            untypedTransformers.Add(propertyName + "Transformers", transformers);

            var name         = propertyName + "Value";
            var xformerField = DefineField(propertyName + "Transformers", typeof(IDataTransformer<T>[]));
            var attrField    = DefineField(propertyName + "Attributes",   typeof(Attribute[]));

            typedTransformers.Add(name, (IDataTransformer[])transformers);

            var res = Expression.Variable(typeof(T), name);
            var exprs = new List<Expression>();
            SourceCode.Replace("return ", "var " + name + " = ");
            exprs.Add(Expression.Assign(res, body));

            for (int i = 0; i < transformers.Length; i++)
            {
                exprs.Add(MarkLine(string.Format("{0} = {1}[{2}].Transform({0}, {3});", name, xformerField.Name, i, attrField.Name)));
                exprs.Add(Expression.Assign(res,
                    Expression.Call(Expression.ArrayIndex(Expression.Field(null, xformerField), Expression.Constant(i)),
                                    typeof(IDataTransformer<T>).GetMethod("Transform"), 
                                    res,
                                    Expression.Field(null, attrField))));
            }

            exprs.Add(MarkLine(string.Format("return {0};", name)));
            exprs.Add(res);
            return Expression.Block(typeof(T), new[] { res }, exprs.ToArray());
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private FieldBuilder DefineField(string name, Type fieldType)
        {
            return typeBuilder.DefineField(
                name,
                fieldType,
                FieldAttributes.Private | FieldAttributes.Static);
        }

        private void AddFields(IEnumerable<string> fieldNames, string fieldTypeName)
        {
            foreach (var name in fieldNames)
            {
                SourceCode.AppendLine()
                          .AppendFormat("        private static {0}[] {1};", fieldTypeName, name);
            }
        }

        private void SetStaticValues<TT>(Type t, Dictionary<string, TT> values)
        {
            foreach (var kv in values)
            {
                t.GetField(kv.Key, BindingFlags.Static | BindingFlags.NonPublic)
                 .SetValue(null, kv.Value);
            }
        }

        private void Dispose(bool isDisposing)
        {
            if (filename != null)
            {
                try
                {
                    File.Delete(filename);
                }
                catch { }
                filename = null;
            }
        }

        private void AppendIndentToSourceCode(bool indentOneMore)
        {
            for (int i = 0; i < indent; i++)
                SourceCode.Append("    ");

            if (indentOneMore)
                SourceCode.Append("    ");
        }
    }
}
