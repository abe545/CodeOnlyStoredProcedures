using System.Data;

namespace CodeOnlyStoredProcedure
{
    internal sealed class StoredProcedureParameter
    {
        public string             Name      { get; set; }
        public DbType             DbType    { get; set; }
        public ParameterDirection Direction { get; set; }
        public int?               Size      { get; set; }
        public byte?              Precision { get; set; }
        public byte?              Scale     { get; set; }
    }
}
