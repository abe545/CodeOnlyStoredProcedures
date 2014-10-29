using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Defines extension methods on the <see cref="StoredProcedure"/> classes.
    /// </summary>
    public static partial class StoredProcedureExtensions
    {
        internal static IEnumerable<StoredProcedureResult> Execute(this IDbCommand cmd, CancellationToken token)
        {
            Contract.Requires(cmd != null);
            Contract.Ensures(Contract.Result<IEnumerable<StoredProcedureResult>>() != null);

            var results = new List<StoredProcedureResult>();
            var reader  = cmd.DoExecute(c => c.ExecuteReader(), token);

            do
            {
                var rows = new List<object[]>();

                while (reader.Read())
                {
                    var values = new object[reader.FieldCount];

                    token.ThrowIfCancellationRequested();
                    reader.GetValues(values);
                    rows.Add(values);
                }

                results.Add(new StoredProcedureResult
                {
                    Rows = rows,
                    ColumnNames = Enumerable.Range(0, reader.FieldCount)
                                            .Select(i => reader.GetName(i))
                                            .ToArray()
                });
            } while (reader.NextResult());

            return results;
        }

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

            var results = cmd.Execute(token);

            token.ThrowIfCancellationRequested();

            if (results.Count() != outputTypes.Count())
                throw new InvalidOperationException("The StoredProcedure " + cmd.CommandText + " returned a different number of result sets than expected. Expected results of type " + outputTypes.Select(t => t.Name).Aggregate((s1, s2) => s1 + ", " + s2));

            return results.Parse(outputTypes, transformers);
        }

        internal static IDictionary<Type, IList> Parse(
            this IEnumerable<StoredProcedureResult> results,
                 IEnumerable<Type>                  outputTypes, 
                 IEnumerable<IDataTransformer>      transformers)
        {
            Contract.Requires(results      != null);
            Contract.Requires(outputTypes  != null && Contract.ForAll(outputTypes,  t => t != null));
            Contract.Requires(transformers != null && Contract.ForAll(transformers, t => t != null));
            Contract.Ensures (Contract.Result<IDictionary<Type, IList>>() != null);

            var spResults = results.ToArray();
            var output    = new Dictionary<Type, IList>();
            int i         = 0;

            foreach (var currentType in outputTypes)
            {
                var res  = spResults[i];
                var rows = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(currentType));
                IRowFactory factory;

                // process the result set
                if (res.ColumnNames.Length == 1 && currentType.IsSimpleType())
                    factory = new SimpleTypeRowFactory(currentType);
                else
                {
                    Type impl = currentType;
                    if (!TypeExtensions.interfaceMap.TryGetValue(currentType, out impl))
                        impl = currentType;

                    Contract.Assume(impl != null);

                    factory = impl.CreateRowFactory();
                }

                foreach (var values in res.Rows)
                {

                    var row = factory.CreateRow(res.ColumnNames, values, transformers);

                    try
                    {
                        rows.Add(row);
                    }
                    catch (ArgumentException)
                    {
                        if (factory is SimpleTypeRowFactory)
                            throw new StoredProcedureColumnException(currentType, row);
                        else
                            throw;
                    }
                }

                if (rows.Count > 0)
                {
                    // throw an exception if the result set didn't include a mapped property
                    if (factory.UnfoundPropertyNames.Any())
                        throw new StoredProcedureResultsException(currentType, factory.UnfoundPropertyNames.ToArray());
                }

                output.Add(currentType, rows);
                ++i;
            }

            return output;
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

        internal static IDbDataParameter AddPrecisison(this IDbDataParameter parameter,
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

        internal static IEnumerable<SqlDataRecord> ToTableValuedParameter(this IEnumerable table, Type enumeratedType)
        {
            Contract.Requires(table != null);
            Contract.Requires(enumeratedType != null);
            Contract.Ensures(Contract.Result<IEnumerable<SqlDataRecord>>() != null);

            var recordList = new List<SqlDataRecord>();
            var columnList = new List<SqlMetaData>();
            var props      = enumeratedType.GetMappedProperties().ToList();

            foreach (var pi in props)
            {
                var attr = pi.GetCustomAttributes(false)
                             .OfType<StoredProcedureParameterAttribute>()
                             .FirstOrDefault();

                if (attr != null)
                    columnList.Add(attr.CreateSqlMetaData(pi.Name, pi.PropertyType));
                else
                    columnList.Add(pi.PropertyType.CreateSqlMetaData(pi.Name, null, null, null, null));
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

        internal class StoredProcedureResult
        {
            public string[]              ColumnNames { get; set; }
            public IEnumerable<object[]> Rows        { get; set; }
        }
    }
}
