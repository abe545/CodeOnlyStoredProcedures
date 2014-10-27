namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Represents a parameter to a stored procedure that returns a value.
    /// </summary>
    public interface IOutputStoredProcedureParameter : IStoredProcedureParameter
    {
        /// <summary>
        /// After a <see cref="StoredProcedure"/> executes, this meethod will be called to transfer the result to
        /// the expectant site.
        /// </summary>
        /// <param name="value">The value returned from the <see cref="StoredProcedure"/>.</param>
        void TransferOutputValue(object value);
    }
}
