using System;
using System.Collections.Generic;
using System.Linq;
using CodeOnlyStoredProcedure;

namespace SmokeTests
{
    partial class Program
    {
        static bool DoGetHierarchicalItemTests(SmokeDb db)
        {
            Console.WriteLine("Calling usp_GetCitiesAndStates synchronously - ");
            var results = db.GetCitiesAndStates.Execute(db.Database.Connection, timeout);

            if (!TestGetStatesResults(results, "usp_GetCitiesAndStates"))
                return false;

            Console.WriteLine("Calling usp_GetStatesAndCities synchronously - ");
            results = db.GetStatesAndCities.Execute(db.Database.Connection, timeout);

            if (!TestGetStatesResults(results, "usp_GetStatesAndCities"))
                return false;

            Console.WriteLine("Calling usp_GetCitiesAndStates synchronously (dynamic syntax) - ");
            results = db.Database.Connection.Execute(timeout).usp_GetCitiesAndStates();

            if (!TestGetStatesResults(results, "usp_GetCitiesAndStates"))
                return false;

            Console.WriteLine("Calling usp_GetStatesAndCities synchronously (dynamic syntax) - ");
            results = db.Database.Connection.Execute(timeout).usp_GetStatesAndCities();

            if (!TestGetStatesResults(results, "usp_GetStatesAndCities"))
                return false;

            return true;
        }

        static bool TestGetStatesResults(IEnumerable<State> results, string storedProc)
        {
            if (results == null)
            {
                WriteError("\tNull returned from " + storedProc);
                return false;
            }

            if (results.Count() != 2)
            {
                WriteError("\tWrong number of rows returned from " + storedProc);
                return false;
            }

            var state = results.First();
            if (state.Name != "New York" || state.Abbreviation != "NY")
            {
                WriteError("\tStates returned in wrong order from " + storedProc);
                return false;
            }

            if (state.Cities == null)
            {
                WriteError("\tCities property was not assigned for New York, when returned from " + storedProc);
                return false;
            }

            if (state.Cities.Count() != 2)
            {
                WriteError("\tWrong number of cities were returned for New York from " + storedProc);
                return false;
            }

            var city = state.Cities.First();
            if (city.Name != "New York" || city.Id != 0 || city.StateId != state.Id)
            {
                WriteError("Cities returned in wrong order from " + storedProc);
                return false;
            }

            city = state.Cities.Last();
            if (city.Name != "Albany" || city.Id != 1 || city.StateId != state.Id)
            {
                WriteError("Cities returned in wrong order from " + storedProc);
                return false;
            }

            state = results.Last();
            if (state.Name != "California" || state.Abbreviation != "CA")
            {
                WriteError("\tStates returned in wrong order from " + storedProc);
                return false;
            }

            if (state.Cities == null)
            {
                WriteError("\tCities property was not assigned for California, when returned from " + storedProc);
                return false;
            }

            if (state.Cities.Count() != 2)
            {
                WriteError("\tWrong number of cities were returned for California from " + storedProc);
                return false;
            }

            city = state.Cities.First();
            if (city.Name != "San Francisco" || city.Id != 2 || city.StateId != state.Id)
            {
                WriteError("\tCities returned in wrong order from " + storedProc);
                return false;
            }

            city = state.Cities.Last();
            if (city.Name != "Los Angeles" || city.Id != 3 || city.StateId != state.Id)
            {
                WriteError("\tCities returned in wrong order from " + storedProc);
                return false;
            }

            WriteSuccess();
            return true;
        }
    }
}
