! Interning Strings

By default, two instances of the exact same string will be duplicated in memory. Ordinarily, this is not an issue. However, sometimes these duplicate strings can take up more memory than you would like. If your {{StoredProcedure}} returns a lot of duplicate strings, you can reduce this memory overhead by calling String.Intern on a given string. Since that is a lot of boilerplate code that most of us wish to avoid (autoprops are great!), we've added the {{Intern}} attribute. By adding it to your string property, the value will be automatically interned for you:

{code:c#}
public class Model
{
    [Intern]
    public string Name { get; set; }
}
{code:c#}

! Interning *All* the Strings

If every string property that your stored procedure returns has a number of duplicates, you can apply global interning to all string properties by calling the {{WithDataTransformer}} method:

{code:c#}
var results = StoredProcedure.Create("usp_getDuplicateStrings")
                             .WithDataTransformer(new InternAllStringsTransformer())
                             .WithResults<string>()
                             .Execute();
{code:c#}