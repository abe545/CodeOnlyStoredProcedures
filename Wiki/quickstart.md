!! Quick Start
{code:c#}
public class Person
{
    [Column("PersonId")]
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

!! Detailed info about using Code Only Stored Procedures
[Using StoredProcedure as a Member]
[Using the Fluent Stored Procedure API]
[Passing Parameters]
[Model Attributes]