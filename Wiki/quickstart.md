!! Quick Start
{code:c#}
public class Person
{
    [Column("PersonId")]
    public int Id { get; set; }
    public DateTime Birthday { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName { get { return FirstName + " " + LastName; } }
}

public class MyModel : DbContext
{
    private readonly StoredProcedure<Person> peopleByBirthday = 
        new StoredProcedure<Person>("dbo", "usp_GetPeopleByBirthday");

    public IEnumerable<Person> GetPeopleByBirthday(DateTime birthday)
    {
        return Database.Connection.Call().usp_GetPeopleByBirthday(Birthday: birthday);
    }

    public Task<IEnumerable<Person>> GetPeopleByLastName(string lastName)
    {
        return Database.Connection.Call().usp_GetPeopleByFamilyName(familyName: lastName);
    }
}
{code:c#}

!! Detailed info about using Code Only Stored Procedures
[Calling stored procedures dynamically]
[Using StoredProcedure as a Member]
[Using the Fluent Stored Procedure API]
[Passing Parameters]
[Model Attributes]