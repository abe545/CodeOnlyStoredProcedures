using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.Linq;

namespace CodeOnlyStoredProcedure
{
    public static partial class StoredProcedureExtensions
    {
        /// <summary>
        /// Clones the given <see cref="StoredProcedure"/>, and sets up the input to pass in the procedure.
        /// </summary>
        /// <typeparam name="TSP">The type of <see cref="StoredProcedure"/> to clone.</typeparam>
        /// <typeparam name="TInput">The type that represents the input to the stored procedure. This can be an anonymous type.</typeparam>
        /// <param name="sp">The <see cref="StoredProcedure"/> to clone.</param>
        /// <param name="input">The input to parse to determine the parameters to pass to the stored procedure. This
        /// can be an anonymous type.</param>
        /// <returns>A clone of the <see cref="StoredProcedure"/> with the input parameters.</returns>
        /// <example>When passing input to a StoredProcedure, you have a few options. You can use WithInput to have more control over
        /// the parameters that are created (by creating a input class, and decorating its properties with the appropriate attributes):
        /// <code language='cs'>
        /// public class MyInputArgs
        /// {
        ///     [StoredProcedureParameter(Name = "bar")]
        ///     public string Foo { get; set; }
        ///     [StoredProcedureParameter(Direction = ParameterDirection.Output)]
        ///     public int BarSize { get; set; }
        /// }
        /// 
        /// var input = new MyInputArgs { Foo = "Baz" };
        /// // after calling execute, input.BarSize will have an output result from the stored procedure.
        /// sp = sp.WithInput(input);
        /// </code>
        /// You can also use WithInput to add a number of parameters at the same time, like so:
        /// <code language='cs'>
        /// sp = sp.WithInput(new { foo = "value", bar = 131.35, baz = DateTime.Now });
        /// </code>
        /// </example>
        public static TSP WithInput<TSP, TInput>(this TSP sp, TInput input)
            where TSP : StoredProcedure
        {
            Contract.Requires(sp                     != null);
            Contract.Requires(input                  != null);
            Contract.Ensures (Contract.Result<TSP>() != null);

            return (TSP)sp.WithInput(input, typeof(TInput));
        }

        internal static StoredProcedure WithInput(this StoredProcedure sp, object input, Type inputType)
        {
            Contract.Requires(sp                                 != null);
            Contract.Requires(input                              != null);
            Contract.Requires(inputType                          != null);
            Contract.Ensures (Contract.Result<StoredProcedure>() != null);

            foreach (var t in inputType.GetParameters(input))
            {
                var pi        = t.Item1;
                var parameter = t.Item2;

                switch (parameter.Direction)
                {
                    case ParameterDirection.Input:
                        sp = sp.CloneWith(parameter);
                        break;

                    case ParameterDirection.InputOutput:
                    case ParameterDirection.Output:
                        sp = sp.CloneWith(parameter, o => pi.SetValue(input, o, null));
                        break;

                    case ParameterDirection.ReturnValue:
                        if (pi.PropertyType != typeof(int))
                            throw new NotSupportedException("Can only use a ReturnValue of type int.");
                        sp = sp.CloneWith(parameter, o => pi.SetValue(input, o, null));
                        break;
                }
            }

            return sp;
        }
    }
}
