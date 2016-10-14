namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Describes the method of quoting objects in the database
    /// </summary>
    public enum ObjectQuoteStyle
    {
        /// <summary>
        /// Uses " to open and close the quotes
        /// </summary>
        DoubleQuote,
        /// <summary>
        /// Uses ` to open and close the quotes
        /// </summary>
        BackTick,
        /// <summary>
        /// Uses [ open a quote, and ] to close it
        /// </summary>
        Brackets
    }
}
