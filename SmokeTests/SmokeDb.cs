using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using CodeOnlyStoredProcedure;

namespace SmokeTests
{
    public class SmokeDb : DbContext
    {
        static SmokeDb()
        {
            Database.SetInitializer<SmokeDb>(null);
        }
    }

    public class Item
    {
        public int    ItemId { get; set; }
        public string Name   { get; set; }
    }

    public class Widget
    {
        public int     WidgetId { get; set; }
        public string  Name     { get; set; }
        public bool?   IsNew    { get; set; }
        public decimal Price    { get; set; }
        public double  Weight   { get; set; }
    }

    public class WidgetComponent
    {
        public int    WidgetComponentId { get; set; }
        public string Name              { get; set; }
    }

    public enum Spoke
    {
        Four    = 4,
        Eight   = 8,
        Sixteen = 16
    }

    [TableValuedParameter(TableName = "Person")]
    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class State
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Abbreviation { get; set; }
        public IEnumerable<City> Cities { get; set; }
    }

    public class City
    {
        public int Id { get; set; }
        public int StateId { get; set; }
        public string Name { get; set; }
    }

    public class ReturnsOne
    {
        [StoredProcedureParameter(Direction = ParameterDirection.ReturnValue)]
        public int ReturnValue { get; set; }
    }

    public class PersonInput
    {
        [TableValuedParameter(TableName = "Person", Name = "people")]
        public IEnumerable<Person> People { get; set; }
    }
}
