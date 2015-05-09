using System;
using System.Diagnostics.Contracts;

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
        /// the parameters that are created (by creating an input class, and decorating its properties with an <see cref="StoredProcedureParameterAttribute"/>).
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
        /// You can also use WithInput to add a number of parameters using an anonymous type.
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

            StoredProcedure temp = sp;
            foreach (var p in typeof(TInput).GetParameters(input))
                temp = temp.CloneWith(p);

            // only do one cast at the end. if the implementation class doesn't implement CloneWith correctly,
            // this will obviously fail, but there really is no need to do the cast after every CloneWith.
            return (TSP)temp;
        }
    }
}
