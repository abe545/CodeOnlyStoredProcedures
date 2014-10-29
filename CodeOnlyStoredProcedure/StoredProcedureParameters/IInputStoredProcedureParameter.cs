namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Represents a parameter that passes input to a <see cref="StoredProcedure"/>.
    /// </summary>
    public interface IInputStoredProcedureParameter : IStoredProcedureParameter
    {
        /// <summary>
        /// Gets the value that will be passed to the <see cref="StoredProcedure"/>.
        /// </summary>
        object Value { get; }
    }
}
