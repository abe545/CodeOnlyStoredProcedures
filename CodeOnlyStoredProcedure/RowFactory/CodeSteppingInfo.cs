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
        private readonly Dictionary<string, Attribute[]>                    typedTransformerAttributes = new Dictionary<string, Attribute[]>();
        private readonly Dictionary<string, DataTransformerAttributeBase[]> transformerAttributes      = new Dictionary<string, DataTransformerAttributeBase[]>();
        private readonly Dictionary<string, Attribute[]>                    allAttributes              = new Dictionary<string, Attribute[]>();
        private readonly Dictionary<string, IDataTransformer[]>             untypedTransformers        = new Dictionary<string, IDataTransformer[]>();
        private readonly Dictionary<string, IDataTransformer[]>             typedTransformers          = new Dictionary<string, IDataTransformer[]>();
        private readonly TypeBuilder typeBuilder;
        private int line   = 8;
        private int indent = 3;
        private string filename;
        private string typeName;

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

            typeName = returnType.GetCSharpName();
            returnType.GetUnderlyingNullableType(out returnType);

            var namespaces = new [] 
            {
                "System", 
                "System.Data",
                "CodeOnlyStoredProcedure", 
                "CodeOnlyStoredProcedure.DataTransformation", 
                returnType.Namespace
            }.Distinct();
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

            SourceCode.AppendLine()
                      .Append("namespace CodeOnlyStoredProcedure.RowFactory_")
                      .AppendLine(id)
                      .AppendLine("{")
                      .AppendLine("    public static class StoredProcedureResultsParser")
                      .AppendLine("    {")
                      .Append("        public static ")
                      .Append(typeName);
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

            var holder = new FunctionDisposer<T>(
                filename,
                (Func<IDataReader, T>)Delegate.CreateDelegate(typeof(Func<IDataReader, T>), t.GetMethod("Parse")));
            return holder.Parse;
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

        public DebugInfoExpression MarkLine(string codeFormat, params object[] formatArguments)
        {
            Contract.Requires(!string.IsNullOrEmpty(codeFormat));
            Contract.Ensures(Contract.Result<DebugInfoExpression>() != null);

            return MarkLine(string.Format(codeFormat, formatArguments));
        }

        public IEnumerable<Expression> AddTransformers(
            IDataTransformer[] transformers, 
            ParameterExpression value, 
            ConstantExpression isNullable, 
            Type targetType,
            Attribute[] attributes, 
            string propertyName)
        {
            Contract.Requires(transformers != null);
            Contract.Requires(value != null);
            Contract.Requires(isNullable != null);
            Contract.Requires(attributes != null);
            Contract.Requires(propertyName != null);
            Contract.Ensures(Contract.Result<IEnumerable<Expression>>() != null);

            allAttributes      .Add(propertyName + "Attributes",   attributes);
            untypedTransformers.Add(propertyName + "Transformers", transformers);

            var xformerField = DefineField(propertyName + "Transformers", typeof(IDataTransformer[]));
            var attrField    = DefineField(propertyName + "Attributes",   typeof(Attribute[]));

            for (int i = 0; i < transformers.Length; i++)
            {
                yield return MarkLine("if ({0}Transformers[{1}].CanTransform({2}, typeof({3}), {4}, {0}Attributes))",
                    propertyName, i, value.Name.ToLower(), targetType.GetCSharpName(), isNullable.Value);
                yield return Expression.IfThen(
                    Expression.Call(Expression.ArrayIndex(Expression.Field(null, xformerField), Expression.Constant(i)),
                        DataTransformerCache.CanTransform,
                        value,
                        Expression.Constant(targetType, typeof(Type)),
                        isNullable,
                        Expression.Field(null, attrField)),
                    Expression.Block(
                        MarkLine(string.Format("{2} = {0}Transformers[{1}].Transform({2}, typeof({3}), {4}, {0}Attributes);",
                            propertyName, i, value.Name.ToLower(), targetType.GetCSharpName(), isNullable.Value), true),
                        Expression.Assign(
                            value,
                            Expression.Call(Expression.ArrayIndex(Expression.Field(null, xformerField), Expression.Constant(i)),
                                DataTransformerCache.Transform,
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

        public Expression AddTransformers(
            DataTransformerAttributeBase[] transformers,
            ParameterExpression retVal,
            Type targetType,
            bool isNullable,
            string name = "attributeTransformers")
        {
            Contract.Requires(transformers != null);
            Contract.Requires(retVal != null);
            Contract.Requires(!string.IsNullOrEmpty(name));
            Contract.Ensures(Contract.Result<Expression>() != null);

            transformerAttributes.Add(name, transformers);
            var args = string.Format(", {0}, {1}", targetType.GetCSharpName(), isNullable);
            return AddTransformers<DataTransformerAttributeBase>(
                name,
                retVal,
                i => i + args,
                transformers,
                DataTransformerCache.AttributeTransform,
                new Expression[] { retVal, Expression.Constant(targetType), Expression.Constant(isNullable) });
        }

        public Expression AddTransformers<T>(
            IDataTransformerAttribute<T>[] transformers, 
            ParameterExpression retVal, 
            string name)
        {
            Contract.Requires(transformers != null);
            Contract.Requires(retVal != null);
            Contract.Requires(!string.IsNullOrEmpty(name));
            Contract.Ensures(Contract.Result<Expression>() != null);

            typedTransformerAttributes.Add(name, (Attribute[])transformers);
            return AddTransformers<IDataTransformerAttribute<T>>(
                name,
                retVal,
                i => i.ToString(),
                transformers,
                DataTransformerCache<T>.AttributeTransform,
                new[] { retVal });
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

            allAttributes      .Add(propertyName + "Attributes",   attributes);
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
                exprs.Add(MarkLine("{0} = {1}[{2}].Transform({0}, {3});", name, xformerField.Name, i, attrField.Name));
                exprs.Add(Expression.Assign(res,
                    Expression.Call(Expression.ArrayIndex(Expression.Field(null, xformerField), Expression.Constant(i)),
                                    DataTransformerCache<T>.Transform, 
                                    res,
                                    Expression.Field(null, attrField))));
            }

            exprs.Add(MarkLine("return {0};", name));
            exprs.Add(res);
            return Expression.Block(typeof(T), new[] { res }, exprs.ToArray());
        }

        private FieldBuilder DefineField(string name, Type fieldType)
        {
            return typeBuilder.DefineField(
                name,
                fieldType,
                FieldAttributes.Private | FieldAttributes.Static);
        }

        private Expression AddTransformers<TTransformer>(
            string              fieldName,
            ParameterExpression retVal,
            Func<int, string>   getTransformArguments,
            TTransformer[]      transformers,
            MethodInfo          transformMethod,
            Expression[]        methodArguments)
        {
            Contract.Requires(!string.IsNullOrEmpty(fieldName));
            Contract.Requires(retVal                       != null);
            Contract.Requires(getTransformArguments        != null);
            Contract.Requires(transformers                 != null);
            Contract.Requires(transformMethod              != null);
            Contract.Requires(methodArguments              != null);
            Contract.Ensures(Contract.Result<Expression>() != null);

            var field = DefineField(fieldName, typeof(TTransformer[]));
            var local = Expression.Variable(field.FieldType, "static_" + fieldName);
            var exprs = new List<Expression>();

            exprs.Add(MarkLine("var static_{0} = {0};", fieldName));
            exprs.Add(Expression.Assign(local, Expression.Field(null, field)));

            for (int i = 0; i < transformers.Length; i++)
            {
                exprs.Add(MarkLine("{0} = static_{1}[{2}].Transform({3});", retVal.Name, fieldName, i, getTransformArguments(i)));
                exprs.Add(Expression.Assign(retVal, Expression.Call(Expression.ArrayIndex(local), transformMethod, methodArguments)));
            }

            return Expression.Block(new[] { local }, exprs);
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

        private void AppendIndentToSourceCode(bool indentOneMore)
        {
            for (int i = 0; i < indent; i++)
                SourceCode.Append("    ");

            if (indentOneMore)
                SourceCode.Append("    ");
        }

        private class FunctionDisposer<T> : IDisposable
        {
            private readonly Func<IDataReader,T> parser;
            private string filename;

            public FunctionDisposer(string filename, Func<IDataReader, T> parser)
            {
                Contract.Requires(!string.IsNullOrEmpty(filename));
                Contract.Requires(parser != null);

                this.filename = filename;
                this.parser   = parser;
            }

            ~FunctionDisposer()
            {
                Dispose(false);
            }

            [DebuggerStepThrough]
            public T Parse(IDataReader reader) { return parser(reader); }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
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
        }
    }
}
