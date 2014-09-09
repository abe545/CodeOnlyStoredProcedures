!! StoredProcedureParameterAttribute
This attribute is used to alter how a parameter is passed to the stored procedure. It is only useful when applied to Properties of a class that you pass into the WithInput extension method.

{code:c#}
public class InputArgs
{
    public int StudentId { get; set; }
    [StoredProcedureParameter(Direction = ParameterDirection.ReturnValue)]
    public int Credits { get; set; }
    [StoredProcedureParameter(Direction = ParameterDirection.Output, SqlDbType = SqlDbType.Char)]
    public string Name { get; set; }
}

public IEnumerable<Classes> GetClasses(int studentId, out int credits, out string name)
{
    var input   = new InputArgs { StudentId = studentId };
    var results = StoredProcedure.Create("usp_getClassesByStudent")
                                 .WithInput(input)
                                 .WithResults<Classes>()
                                 .Execute(this.Database.Connection);

    credits = input.Credits;
    name    = input.Name;

    return results;
}
{code:c#}

!! DataTransformerAttributeBase
When an Attribute derived from {{DataTransformerAttributeBase}} is applied to a property on the Model, it will be used to transform the data before being set on the property. There are built-in transformers for [Automatically Converting Numeric Types], [Interning Strings], and [Trimming Strings]. If you have any suggestions for more data transformations, please let me know!