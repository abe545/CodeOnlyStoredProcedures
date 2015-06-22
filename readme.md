# Code Only Stored Procedures
[![Build status](https://ci.appveyor.com/api/projects/status/j9beupy3qo611kkc/branch/master?svg=true)](https://ci.appveyor.com/project/abe545/codeonlystoredprocedures/branch/master)

A library for easily calling Stored Procedures in .NET. Works great with Entity Framework Code First models. 
Code Only Stored Procedures will not create any Stored Procedures on your database. Instead, its aim is to make it easy to call your existing stored procedures by writing simple code.

This library is released via NuGet. You can go to the [CodeOnlyStoredProcedures NuGet Page](https://www.nuget.org/packages/CodeOnlyStoredProcedures) for more information on releases, or just grab it:

```
Install-Package CodeOnlyStoredProcedures
```

## Usage
There are a number ways you can use this library to make working with stored procedures easier. First, you're going to need a connection to a Database. If you're using Entity Framework, you can grab it from the `Database.Connection` on your `DbContext`. Otherwise, you can create a new connection however you want. Once you have an `IDbConnection db`, you can start executing those stored procs:

#### Get Results in One line
The easiest way is to use the dynamic syntax:

```cs
IEnumerable<Person> people = db.Execute().usp_GetPeople();
```

#### What if my Stored Procedure returns multiple result sets?
They will return a tuple:

```cs
Tuple<IEnumerable<Person>, IEnumerable<Family>> results = db.Execute().usp_GetFamilies();
```

Or, if you want to use the fluent syntax:

```cs
var results = StoredProcedure.Create("usp_GetFamilies")
    .WithResults<Person, Family>()
    .Execute(connection);
```

#### But, they are hierarchical...
The library will try to build the hierarchies for you, by following these rules

1. The parent model should contain an enumerable property with the child type
  * It can be an array - `Child[]`
  * It can be any generic enumerable type - `IEnumerable<Child>`, `IList<Child>`, `ICollection<Child>`, etc.
1. Each model should have a property named `Id` or `{ClassName}Id`
  * If the property isn't named Id or `{ClassName}Id`, you can decorate the property that should be used as the Id with the KeyAttribute - `[Key] MyId { get; set; }`
1. The child model should have a property named `{ParentClass}Id`
  * If the child's foreign key isn't `{ParentClass}Id`, you should decorate the enumerable child property with the ForeignKeyAttribute - `[ForeignKey("MyParentId")] IEnumerable<Child> Children { get; set; }`

For example, these would all work:

```cs
public class Parent
{
    public int Id { get; set; }
    public IEnumerable<Child> Children { get; set; }
}
public class Child
{
    public int ParentId { get; set; }
}
```

```cs
public class Parent
{
    [Key]
    public int Property { get; set; }
    public IEnumerable<Child> Children { get; set; }
}
public class Child
{
    public int ParentId { get; set; }
}
```

```cs
public class Parent
{
    public int Id { get; set; }
    [ForeignKey("ParentPropertyKey")]
    public IEnumerable<Child> Children { get; set; }
}
public class Child
{
    public int ParentPropertyKey { get; set; }
}
```

You can then get the hierarchical items like so: `IEnumerable<Parent> res = db.Execute().usp_GetParentsAndChildren();`

#### But, I like control (or it can't figure out how to parse my hierarchical results)
You can declare the order of the result sets using the fluent syntax:

```cs
IEnumerable<Second> res = StoredProcedure.Create("usp_GetHierarchy")
                                         .WithResults<First, Second, Third>()
                                         .AsHierarchical<Second>()
                                         .Execute(connection);
```

#### Want it asynchronous?
Just use `ExecuteAsync` it that way.

```cs
Task<IEnumerable<Person>> task = db.ExecuteAsync().usp_GetPeople();
```

#### Rather await it?
Using .NET 4.5 (or the Async NuGet package in 4.0)? That's easy too.

```cs
IEnumerable<Person> people = await db.Execute().usp_GetPeople();
```

#### Using the repository pattern?
Even easier. Your interface is basically doing the work for you.

```cs
public IEnumerable<Person> GetPeople()
{
    return this.db.Execute().usp_GetPeople();
}

public Task<IEnumerable<Person>> GetPeopleAsync()
{
    return this.db.ExecuteAsync().usp_GetPeople();
}
```

#### What about cancellation?
All those tasks do need to be cancellable. So, pass your token

```cs
public Task<IEnumerable<Person>> GetPeopleAsync(CancellationToken token)
{
    return this.db.ExecuteAsync(token).usp_GetPeople();
}
```

#### What if my database is slow?
Sometimes, you need to have a longer execution timeout. Just tell us how many seconds you need.

```cs
IEnumerable<Widget> widgets = db.Execute(3600).sp_getAllTheWidgetsInTheWorld();
```

#### But my procedure takes input!
Okay, so send it in!

```cs
IEnumerable<Widget> widgets = db.Execute().sp_getWidgets(weight: 42, name: "Frob");
```

#### What about output parameters?
Those work. So do Input/Output parameters (you know these keywords, right?).

```cs
int count;
int smallest = 15;
db.Execute().sp_getWidgetCount(count: out count, smallest_widget: ref smallest);
```

#### And if I need the return value?
This one isn't that tough. It is just an out parameter with a special (case-INseNsiTivE) name.

```cs
int count;
db.Execute().sp_getWidgetCount(ReturnValue: out count);
```

#### How can those parameters work when called asynchronously?
They can't. At least, not the simple way. You can declare a helper class though.

```cs
private class WidgetParameters
{
    [StoredProcedureParameter(Direction = ParameterDirection.ReturnValue)]
    public int Count { get; set; }
    [StoredProcedureParameter("smallest_widget", Direction = ParameterDirection.ReturnValue)]
    public int Smallest { get; set; }
}

var input = new WidgetParameters { Smallest = 15 };
await db.ExecuteAsync().sp_getWidgetCount(input);
```

#### I hate dynamically typed C#!
Some people hate magically typed C#. I get it. Luckily, you can use magic strings instead!

```cs
var sp = new StoredProcedure<Person>("dbo", "usp_GetPeople");
var people = sp.Execute(db);
```

#### So, what if I don't like that syntax?
I aim to please. There is a fluent syntax also.

```cs
var people = StoredProcedure.Create("usp_getPeople")
                            .WithResults<People>()
                            .Execute(db);
```

#### What if I like async though?
Either the fluent syntax or the class syntax can execute asynchronously:

```cs
Task<IEnumerable<Person>> StoredProcedure.Create("usp_getPeople")
                                         .WithResults<People>()
                                         .ExecuteAsync(token); // yep, you an cancel it!
```

#### How do I pass parameters this way?
Well, it isn't as easy, but still pretty simple.
_It is important to know that a `StoredProcedure` is immutable. This means that if you call any of the following methods on an instance of the class, the original reference will not be modified._

```cs
var people = StoredProcedure.Create("usp_getPeople")
                            .WithResults<People>()
                            .WithParameter("name", "Bob")
                            .Execute(db);
```

#### What about return values?
You must pass an `Action<int>` (be careful if you call this asynchronously that you don't access the result until after the task completes).
```cs
int retVal;
StoredProcedure.Create("sp_getWidgetCount")
               .WithReturnValue(rv => retVal = rv)
               .Execute(db);
```

#### You know we need (input/)ouput parameters, right?
Yep, and you can use them similarly to return values.

```cs
int count, smallest;
StoredProcedure.Create("sp_getWidgetCount")
               .WithOutputParameter("count", i => count = i)
               .WithInputOutputParameter("smallest_widget", 15, i => smallest = i)
               .Execute(db);
```

#### That seems like a lot to keep track of
You can create an input class like with the dynamic syntax. It might be easier to pass out or in/out parameters this way.

```cs

private class WidgetParameters
{
    [StoredProcedureParameter(Direction = ParameterDirection.ReturnValue)]
    public int Count { get; set; }
    [StoredProcedureParameter("smallest_widget", Direction = ParameterDirection.ReturnValue)]
    public int Smallest { get; set; }
}

var input = new WidgetParameters { Smallest = 15 };
StoredProcedure.Create("sp_getWidgetCount")
               .WithInput(input)
               .Execute(db);
```

#### So, what are these data transformations?
Sometimes, databases return data in a form that isn't _quite_ what you want. These help correct that.
You can apply them to model properties individually

```cs
public class Person
{
    [Trim]
    public string FirstName { get; set; }
    [Trim, Intern]
    public string LastName  { get; set; }
}
```

Or, if you want to apply the same transformer to all properties of a type

```cs
var people = StoredProcedure.Create("usp_getPeople")
                            .WithResults<People>()
                            .WithDataTransformer(new TrimAllStringsTransformer())
                            .Execute(db);
```

Currently, there are transformers for interning strings, trimming strings, and automatically converting numeric types (like `double` to `decimal`). These are extensible, in case you have additional use cases.

#### What if the column names are dumb?
You can rename them on your model.

```cs
public class Person
{
    [Column("ResultId")]
    public int Id { get; set; }
}
```

#### Can I also use table valued input parameters?
Sure, you can again choose between dynamic or fluent syntax. This is an example in fluent:

```cs
IEnumerable<InputRow> rows = ...;
StoredProcedure.Create("dbo", "usp_TakesLotsOfInput")
               .WithTableValuedParameter("parameterName", rows, "schemaOfTable", "typeOfTable")
               .Execute(dbConnection);

[TableValuedParameter(Schema = "schemaOfTable", TableName = "typeOfTable")]
public class InputRow { ... }
```
