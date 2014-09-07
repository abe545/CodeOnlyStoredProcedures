! Automatically Converting Numeric Types

You can easily massage numeric data types by applying the {{ConvertNumeric}} attribute to your property:

{code:c#}
public class Model
{
    [ConvertNumeric]
    public decimal Price { get; set; }
}
{code:c#}

Doing this will enable your StoredProcedure to return a different data type than you use in your code.

!! Reason for this feature
Sometimes you want to represent your data slightly differently in code than you do on the database. Or, perhaps you don't have control over the Stored Procedure to get the correct data type returned. It is a hassle to declare a property that isn't really used by your code to get the data in the correct type, only to manually cast the result and set the property you care about. It would be nice if you could just set the type of the property you want to use. 

For example, if your db returns a {{double}}, and you actually want a {{decimal}}, this throws an {{InvalidCastException}}:

{code:c#}
public class Model
{
    // The db returns a double, so this won't work
    public decimal Price { get; set; }
}
{code:c#}

You *could* do this:

{code:c#}
public class Model
{
    [NotMapped]
    public decimal Price { get; set; }
    [Column("Price")]
    public double InternalPrice
    {
        get { return (double)Price; }
        set { Price = (decimal)value; }
    }
}
{code:c#}

If you have an interface, explicitly implementing the properties where these collisions occur is slightly less code, but it is still annoying. And easier to screw up than just using the {{ConvertNumeric}} attribute.