! Code Only Stored Procedures
{project:description}

Code Only Stored Procedures will not create any Stored Procedures on your database. Instead, its aim is to make it easy to call your existing stored procedures by writing simple code.

This library will be released solely on NuGet. You can go to the [url:CodeOnlyStoredProcedures NuGet Page|https://www.nuget.org/packages/CodeOnlyStoredProcedures] for more information on releases, or just grab it:

{code:powershell}
Install-Package CodeOnlyStoredProcedures
{code:powershell}

!! Quick Start
{code:c#}
public class Person
{
    [Column("ResultId")]
    public int Id { get; set; }
    public DateTime Birthday { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    [NotMapped]
    public string FullName { get { return FirstName + " " + LastName; } }
}

public class MyModel : DbContext
{
    private readonly StoredProcedure<Person> peopleByBirthday = 
        new StoredProcedure<Person>("dbo", "usp_GetPeopleByBirthday");

    public IEnumerable<Person> GetPeopleByBirthday(DateTime birthday)
    {
        return peopleByBirthday.WithInput(new { Birthday = birthday })
            .Execute(this.Database.Connection);
    }

    public Task<IEnumerable<Person>> GetPeopleByLastName(string lastName)
    {
        // schema defaults to "dbo"
        return StoredProcedure.Create("usp_GetPeopleByFamilyName")
            .WithInput("familyName", lastName)
            .WithResult<Person>()
            .Execute(this.Database.Connection);
    }
}
{code:c#}

!! Dynamic Syntax
The new dynamic syntax in 1.2 makes calling your stored procedures a LOT easier

{code:c#}
public class DynamicModel : DbContext
{
    // Just by specifying the result type and parameters, we are able to figure out
    // how to call the query.
    public IEnumerable<Person> GetPeopleByBirthday(DateTime birthday)
    {
        return Database.Connection.Call().usp_GetPeopleByBirthday(Birthday: birthday);
    }

    // If you specify the result as a Task, the procedure will be executed asynchronously
    public Task<IEnumerable<Person>> GetPeopleByLastName(string lastName)
    {
        return Database.Connection.Call().usp_GetPeopleByFamilyName(familyName: lastName);
    }
}
{code:c#}