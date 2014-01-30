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
        public StoredProcedure                          ReturnsOne { get; private set; }
        public StoredProcedure<Item>                    GetItems   { get; private set; }
        public StoredProcedure<Item>                    GetItem    { get; private set; }
        public StoredProcedure<Widget, WidgetComponent> GetWidget  { get; private set; }

        static SmokeDb()
        {
            Database.SetInitializer<SmokeDb>(null);
        }

        public SmokeDb()
        {
            ReturnsOne = new StoredProcedure                         ("usp_ReturnsOne");
            GetItems   = new StoredProcedure<Item>                   ("usp_GetItems");
            GetItem    = new StoredProcedure<Item>                   ("usp_GetItem");
            GetWidget  = new StoredProcedure<Widget, WidgetComponent>("usp_GetWidget");
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
}
