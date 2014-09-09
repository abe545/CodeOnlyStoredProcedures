! Fluent Api
You can call the Stored Procedure using a fluent syntax. You start with the StoredProcedure.Create method:

{code:c#}
public class MyDataBase : DbContext
{    
    public void CallMyStoredProc()
    {
        StoredProcedure.Create("MyStoredProc")
                       .Execute(this.Database.Connection);
    }

    public Task CallMyStoredProcAsync()
    {
        return StoredProcedure.Create("schema", "MyStoredProc")
                              .ExecuteAsync(this.Database.Connection);
    }
}
{code:c#}

!! Results
If your procedure returns one or more result sets (up to 7), you use the WithResults method:

{code:c#}
public class MyDataBase : DbContext
{    
    public Tuple<IEnumerable<Results1>, IEnumerable<Results2>> CallMyStoredProc()
    {
        return StoredProcedure.Create("MyStoredProc")
                              .WithResults<Results1, Results2>()
                              .Execute(this.Database.Connection);
    }

    public Task<IEnumerable<Results>> CallMyStoredProcAsync()
    {
        return StoredProcedure.Create("schema", "MyStoredProc")
                              .WithResults<Results>()
                              .ExecuteAsync(this.Database.Connection);
    }
}
{code:c#}

!! Input / Output Parameters (and Return Values)
You can pass input parameters (and retrieve output parameters or return values) with either of the methods shown in [Passing Input Parameters].

!! Data Transformations
Sometimes the data returned from the Database isn't exactly what you'd want to use in your model. You can massage the data returned by passing an instance of `IDataTransformer`:

{code:c#}
public class ToUpperTransformer : IDataTransformer
{
    public bool CanTransform(object value, Type targetType, IEnumerable<Attribute> propertyAttributes)
    {
        return value is string;
    }

    public object Transform(object value, Type targetType, IEnumerable<Attribute> propertyAttributes)
    {
        return ((string)value).ToUpper();
    }
}

StoredProcedure.Create("dbo", "MyStoredProc")
               .WithDataTransformer(new ToUpperTransformer());
{code:c#}