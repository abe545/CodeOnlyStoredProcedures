using CodeOnlyStoredProcedure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmokeTests
{
    public class SmokeDb : DbContext
    {
        public StoredProcedure                          ReturnsOne         { get; private set; }
        public StoredProcedure<Item>                    GetItems           { get; private set; }
        public StoredProcedure<Item>                    GetItem            { get; private set; }
        public StoredProcedure<Widget, WidgetComponent> GetWidget          { get; private set; }
        public StoredProcedure<int>                     GetSpokes          { get; private set; }
        public StoredProcedure<Spoke>                   GetSpokes2         { get; private set; }
        public StoredProcedure<Person>                  GetExistingPeople  { get; private set; }
        public StoredProcedure<State>                   GetStatesAndCities { get; private set; }
        public StoredProcedure<State>                   GetCitiesAndStates { get; private set; }

        static SmokeDb()
        {
            Database.SetInitializer<SmokeDb>(null);
        }

        public SmokeDb()
        {
            ReturnsOne         = new StoredProcedure                         ("usp_ReturnsOne");
            GetItems           = new StoredProcedure<Item>                   ("usp_GetItems");
            GetItem            = new StoredProcedure<Item>                   ("usp_GetItem");
            GetWidget          = new StoredProcedure<Widget, WidgetComponent>("usp_GetWidget");
            GetSpokes          = new StoredProcedure<int>                    ("usp_GetSpokes");
            GetSpokes2         = new StoredProcedure<Spoke>                  ("usp_GetSpokes");
            GetExistingPeople  = new StoredProcedure<Person>                 ("usp_GetExistingPeople");
            GetStatesAndCities = new StoredProcedure<State>                  ("usp_GetStatesAndCities");
            GetCitiesAndStates = new StoredProcedure<State>                  ("usp_GetCitiesAndStates");
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
}
