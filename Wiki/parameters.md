!! Input parameter
{code:c#}
StoredProcedure.Create("dbo", "MyStoredProc")
               .WithParameter("Key", 100)
               .WithParameter("Date", DateTime.Now);
{code:c#}

!! Output parameter
{code:c#}
string output = null;
StoredProcedure.Create("dbo", "MyStoredProc")
               .WithOutputParameter("Name", s => output = s);
{code:c#}

!! Input/Output parameter
{code:c#}
string output = null;
StoredProcedure.Create("dbo", "MyStoredProc")
               .WithInputOutputParameter("Name", "foo", s => output = s);
{code:c#}

!! Table Valued parameter
{code:c#}
IEnumerable<Person> people;
StoredProcedure.Create("dbo", "MyStoredProc")
               .WithTableValuedParameter("newPeople", people);
{code:c#}

!! Return Value
If your procedure has a meaningful return value, you can use the WithReturnValue method:

{code:c#}
int count = -1;
StoredProcedure.Create("dbo", "MyStoredProc")
               .WithReturnValue(i => count = i);
{code:c#}

!! All parameters by passing a (possibly anonymous) type
{code:c#}
StoredProcedure.Create("dbo", "MyStoredProc")
               .WithInput(new { Key = 100, Date = DateTime.Now });
{code:c#}

If you need any type of parameter other than an input parameter, you can create a class to do so (a struct would also work for input parameters, but since structs are passed by copy, there would be no way to get output from the StoredProcedure):

{code:c#}
public class MyStoredProcParameters
{
    [StoredProcedureParameter(Direction = ParameterDirection.InputOutput)]
    public string Name { get; set; }
    [StoredProcedureParameter(Direction = ParameterDirection.ReturnValue)]
    public int ResultCode { get; set; }
    [StoredProcedureParameter(SqlDbType = SqlDbType.Structured)]
    public IEnumerable<Person> People { get; set; }
}

var parms = new MyStoredProcParameters { Name = "foo", People = people };
StoredProcedure.Create("dbo", "MyStoredProc")
               .WithInput(parms)
               .Execute(this.Database.Connection);
// parms.Name will be updated, as will the ResultCode
{code:c#}