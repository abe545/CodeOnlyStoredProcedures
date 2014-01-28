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
        public StoredProcedure       ReturnsOne { get; private set; }
        public StoredProcedure<Item> GetItems   { get; private set; }
        public StoredProcedure<Item> GetItem    { get; private set; }

        static SmokeDb()
        {
            Database.SetInitializer<SmokeDb>(null);
        }

        public SmokeDb()
        {
            ReturnsOne = new StoredProcedure      ("usp_ReturnsOne");
            GetItems   = new StoredProcedure<Item>("usp_GetItems");
            GetItem    = new StoredProcedure<Item>("usp_GetItem");
        }
    }

    public class Item
    {
        [Key]
        public int    ItemId { get; set; }
        public string Name   { get; set; }
    }
}
