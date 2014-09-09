! Trimming Strings

Sometimes database data is messy. For instance, strings get entered with trailing (or leading) spaces. This can cause a lot of headaches when programming things like equality. Apparently {{"foo" != "foo "}}! Because of this, we have added the {{Trim}} attribute for model properties:

{code:c#}
public class Model
{
    [Trim]
    public string Name { get; set; }
}
{code:c#}

! Trimming *All* the Strings

If you have a lot of messy strings, and no leading/trailing whitespace is important to you, you can apply a global string trimmer:

{code:c#}
var results = StoredProcedure.Create("usp_getDuplicateStrings")
                             .WithDataTransformer(new TrimAllStringsTransformer())
                             .WithResults<string>()
                             .Execute();
{code:c#}