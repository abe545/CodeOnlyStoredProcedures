! Using StoredProcedure as a Member
You can declare your stored procedure as a member of your class. If using the Entity Framework 5 or higher, you should use a Code First DbContext:

{code:c#}
public class MyDataBase : DbContext
{
    private readonly myStoredProc = new StoredProcedure("dbo", "MyStoredProc");
    
    public void CallMyStoredProc()
    {
        myStoredProc.Execute(this.Database.Connection);
    }

    public Task CallMyStoredProcAsync()
    {
        return myStoredProc.ExecuteAsync(this.Database.Connection);
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
    private readonly myStoredProc<MyResults> = 
        new StoredProcedure<MyResults>("dbo", "MyStoredProc");
    private readonly myStoredProc<MyResults, MyResults> = 
        new StoredProcedure<MyResults, MyResults>("dbo", "MyStoredProc2");
        
    public IEnumerable<MyResults>CallMyStoredProc()
    {
        return myStoredProc.Execute(this.Database.Connection);
    }

    public Task<IEnumerable<MyResults>> CallMyStoredProcAsync()
    {
        return myStoredProc.ExecuteAsync(this.Database.Connection);
    }

    public Tuple<IEnumerable<MyResults>, IEnumerable<MyResults>> CallMyStoredProc2()
    {
        return myStoredProc.Execute(this.Database.Connection);
    }

    public Task<Tuple<IEnumerable<MyResults>, IEnumerable<MyResults>>> CallMyStoredProc2Async()
    {
        return myStoredProc.ExecuteAsync(this.Database.Connection);
    }
}
{code:c#}

You can pass input parameters with either of the [Passing Parameters] methods. If you always pass the same parameter to a Stored Procedure, you can use the result of the {{WithParameter}} method as your member variable, and you won't have to set the value every time you call {{Execute}}:

{code:c#}
private readonly myStoredProc<MyResults> = 
    new StoredProcedure<MyResults>("dbo", "MyStoredProc")
            .WithParameter("parameter", "myApp");

{code:c#}
