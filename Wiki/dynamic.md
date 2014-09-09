! Calling stored procedures dynamically
This new (in 1.2) syntax makes calling your stored procedures **much** easier. It removes all the extra code required to call your stored procedures.

{code:c#}
public class MyDataBase : DbContext
{
    public void CallMyStoredProc()
    {
        this.Database.Connection.Call().MyStoredProc();
    }

    public Task CallMyStoredProcAsync()
    {
        return this.Database.Connection.Call().MyStoredProc();
    }
}
{code:c#}

If your stored procedure returns one or more result sets (up to 7), you can declare them just like a code first data model:

{code:c#}
public class MyResults
{
    [Column("ResultId")]
    public int Key { get; set; }
    public string Name { get; set; }
}

public class MyDataBase : DbContext
{        
    public IEnumerable<MyResults> CallMyStoredProc()
    {
        return this.Database.Connection.Call().MyStoredProc();
    }

    public Task<IEnumerable<MyResults>> CallMyStoredProcAsync()
    {
        return this.Database.Connection.Call().MyStoredProc();
    }

    public Tuple<IEnumerable<MyResults>, IEnumerable<MyResults>> CallMyStoredProc2()
    {
        return this.Database.Connection.Call().MyStoredProc2();
    }

    public Task<Tuple<IEnumerable<MyResults>, IEnumerable<MyResults>>> CallMyStoredProc2Async()
    {
        return this.Database.Connection.Call().MyStoredProc2();
    }
}
{code:c#}

You can easily pass parameters also:

{code:c#}
public class MyDataBase : DbContext
{        
    public IEnumerable<MyResults> CallMyStoredProc(int id)
    {
        return this.Database.Connection.Call().MyStoredProc(id: id);
    }
        
    // Output parameter
    public IEnumerable<MyResults> CallMyStoredProc(out int count)
    {
        return this.Database.Connection.Call().MyStoredProc(count: out count);
    }
        
    // Input/output parameter
    public IEnumerable<MyResults> CallMyStoredProc(ref int id)
    {
        return this.Database.Connection.Call().MyStoredProc(id: ref id);
    }

    // return values are just out parameters with a special name    
    public IEnumerable<MyResults> CallMyStoredProc(out int returnValue)
    {
        return this.Database.Connection.Call().MyStoredProc(returnValue: returnValue);
    }

    // ref/out parameters can't be called in async methods, but you can
    // pass an input object
    public class MyInput
    {
        [StoredProcedureParameter(Direction = ParameterDirection.ReturnValue)]
        public int ReturnValue { get; set; }
        [StoredProcedureParameter(Direction = ParameterDirection.Output)]
        public int Count       { get; set; }
        [StoredProcedureParameter(Direction = ParameterDirection.InputOutput)]
        public int Id          { get; set; }
    }
        
    public Task<IEnumerable<MyResults>> CallMyStoredProc(MyInput input)
    {
        return this.Database.Connection.Call().MyStoredProc(input);
    }
}
{code:c#}