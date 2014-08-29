using Microsoft.SqlServer.Server;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Defines extension methods on the <see cref="StoredProcedure"/> classes.
    /// </summary>
    public static partial class StoredProcedureExtensions
    {
        private static readonly Type[] integralTpes = new[]
            {
                typeof(String),
                typeof(Int16),
                typeof(Int32),
                typeof(Int64),
                typeof(Decimal),
                typeof(Double),
                typeof(Single),
                typeof(Boolean),
                typeof(Byte),
                typeof(DateTime),
                typeof(Guid)
            };

        internal static IDictionary<Type, IList> Execute(
            this IDbCommand               cmd,
            CancellationToken             token,
            IEnumerable<Type>             outputTypes,
            IEnumerable<IDataTransformer> transformers)
        {
            Contract.Requires(cmd          != null);
            Contract.Requires(outputTypes  != null && Contract.ForAll(outputTypes, t => t != null));
            Contract.Requires(transformers != null);
            Contract.Ensures (Contract.Result<IDictionary<Type, IList>>() != null);

            var first   = true;
            var results = new Dictionary<Type, IList>();
            var reader  = cmd.DoExecute(c => c.ExecuteReader(), token);

            token.ThrowIfCancellationRequested();

            foreach (var currentType in outputTypes)
            {
                if (first)
                    first = false;
                else if (!reader.NextResult())
                    throw new InvalidOperationException("The StoredProcedure returned a different number of result sets than expected. Expected results of type " + outputTypes.Select(t => t.Name).Aggregate((s1, s2) => s1 + ", " + s2));
                
                // process results - repeat this loop for each result set returned
                // by the stored proc for which we have a result type specified
                var values = new object[reader.FieldCount];
                var output = (IList)Activator.CreateInstance (typeof(List<>).MakeGenericType(currentType));

                IRowFactory factory;

                // process the result set
                if (reader.FieldCount == 1 && (currentType.IsEnum || integralTpes.Contains(currentType)))
                    factory = new SimpleTypeRowFactory(currentType);
                else
                    factory = currentType.CreateRowFactory();

                var names = Enumerable.Range(0, reader.FieldCount)
                                        .Select(i => reader.GetName(i))
                                        .ToArray();

                while (reader.Read())
                {
                    token .ThrowIfCancellationRequested();
                    reader.GetValues(values);

                    var row = factory.CreateRow(names, values, transformers);

                    try
                    {
                        output.Add(row);
                    }
                    catch (ArgumentException)
                    {
                        if (factory is SimpleTypeRowFactory)
                            throw new StoredProcedureColumnException(currentType, row);
                        else
                            throw;
                    }
                }

                if (output.Count > 0)
                {
                    // throw an exception if the result set didn't include a mapped property
                    if (factory.UnfoundPropertyNames.Any())
                        throw new StoredProcedureResultsException(currentType, factory.UnfoundPropertyNames.ToArray());
                }

                results.Add(currentType, output);
            }

            reader.Close();
            return results;
        }

        internal static T DoExecute<T>(this IDbCommand cmd, Func<IDbCommand, T> exec, CancellationToken token)
        {
            Contract.Requires(cmd  != null);
            Contract.Requires(exec != null);
            Contract.Ensures (Contract.Result<T>() != null);

            // execute in a background task, so we can cancel the command if the 
            // CancellationToken is cancelled, or the command times out
            var readerTask = Task.Factory.StartNew(() => exec(cmd), token, TaskCreationOptions.None, TaskScheduler.Default);

            try
            {
                var timeout = cmd.CommandTimeout;
                if (timeout > 0)
                {
                    if (!readerTask.Wait(timeout * 1000, token) && !readerTask.IsCompleted)
                    {
                        cmd.Cancel();
                        throw new TimeoutException();
                    }

                    if (token.IsCancellationRequested && !readerTask.IsCompleted)
                        cmd.Cancel();
                }
                else
                    readerTask.Wait(token);
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.Count() == 1)
                {
                    if (!(ex.InnerException is OperationCanceledException))
                        throw;
                    else
                        cmd.Cancel();
                }
                else
                    throw;
            }
            catch (OperationCanceledException)
            {
                cmd.Cancel();
                throw;
            }

            token.ThrowIfCancellationRequested();

            var res = readerTask.Result;
            if (res == null)
                throw new NotSupportedException("The stored procedure did not return any results.");

            return res;
        }

        #region Private Helpers
        private static SqlParameter AddPrecisison(this SqlParameter parameter,
            int? size,
            byte? scale,
            byte? precision)
        {
            Contract.Requires(parameter != null);

            if (size.HasValue)      parameter.Size      = size.Value;
            if (scale.HasValue)     parameter.Scale     = scale.Value;
            if (precision.HasValue) parameter.Precision = precision.Value;

            return parameter;
        }

        static IEnumerable<SqlDataRecord> ToTableValuedParameter(this IEnumerable table, Type enumeratedType)
        {
            Contract.Requires(table != null);
            Contract.Requires(enumeratedType != null);
            Contract.Ensures(Contract.Result<IEnumerable<SqlDataRecord>>() != null);

            var recordList = new List<SqlDataRecord>();
            var columnList = new List<SqlMetaData>();
            var props      = enumeratedType.GetMappedProperties().ToList();
            string name;
            SqlDbType coltype;

            foreach (var pi in props)
            {
                var attr = pi.GetCustomAttributes(false)
                             .OfType<StoredProcedureParameterAttribute>()
                             .FirstOrDefault();
                if (attr != null && !string.IsNullOrWhiteSpace(attr.Name))
                    name = attr.Name;
                else
                    name = pi.Name;

                if (attr != null && attr.SqlDbType.HasValue)
                    coltype = attr.SqlDbType.Value;
                else
                    coltype = pi.PropertyType.InferSqlType();

                SqlMetaData column;
                switch (coltype)
                {
                    case SqlDbType.Binary:
                    case SqlDbType.Char:
                    case SqlDbType.NChar:
                    case SqlDbType.Image:
                    case SqlDbType.VarChar:
                    case SqlDbType.NVarChar:
                    case SqlDbType.Text:
                    case SqlDbType.NText:
                    case SqlDbType.VarBinary:
                        var size = 50;
                        if (attr != null && attr.Size.HasValue)
                            size = attr.Size.Value;
                        column = new SqlMetaData(name, coltype, size);
                        break;

                    case SqlDbType.Decimal:
                        byte precision = 10;
                        byte scale = 2;

                        if (attr != null)
                        {
                            if (attr.Precision.HasValue) precision = attr.Precision.Value;
                            if (attr.Scale.HasValue)     scale     = attr.Scale.Value;
                        }
                        column = new SqlMetaData(name, coltype, precision, scale);
                        break;

                    default:
                        column = new SqlMetaData(name, coltype);
                        break;
                }

                columnList.Add(column);
            }

            // copy the input list into a list of SqlDataRecords
            foreach (var row in table)
            {
                var record = new SqlDataRecord(columnList.ToArray());
                for (int i = 0; i < columnList.Count; i++)
                {
                    // locate the value of the matching property
                    var value = props[i].GetValue(row, null);
                    record.SetValue(i, value);
                }

                recordList.Add(record);
            }

            return recordList;
        }
        #endregion
    }
}
