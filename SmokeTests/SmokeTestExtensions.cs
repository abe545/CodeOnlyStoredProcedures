using System;
using System.Collections.Generic;
using System.Linq;

namespace SmokeTests
{
    public static class SmokeTestExtensions
    {
        public static Tuple<bool, string> TestGetItemsResults(this IEnumerable<Item> res)
        {
            if (res == null)
                return Tuple.Create(false, "\tNull returned from usp_GetItems.");

            if (res.Count() != 2)
                return Tuple.Create(false, "\tWrong number of results returned from usp_GetItems.");

            var item = res.First();
            if (item.ItemId != 0 || item.Name != "Foo")
                return Tuple.Create(false, string.Format("\tIncorrect first item.\n\t\tExpected 'Foo' [0]\n\t\tActual '{0}' [{1}]", item.Name, item.ItemId));

            item = res.Last();
            if (item.ItemId != 1 || item.Name != "Bar")
                return Tuple.Create(false, string.Format("\tIncorrect last item.\n\t\tExpected 'Bar' [1]\n\t\tActual '{0}' [{1}]", item.Name, item.ItemId));

            return Tuple.Create(true, "");
        }

        public static Tuple<bool, string> TestGetItemResults(this IEnumerable<Item> items, int expectedId = 0, string expectedName = "Foo")
        {
            if (items.Count() != 1)
                return Tuple.Create(false, "\tusp_GetItem returned more than one item");

            var item = items.Single();
            if (item.ItemId != expectedId || item.Name != expectedName)
                return Tuple.Create(false, "\tusp_GetItem returned the wrong item");

            return Tuple.Create(true, "");
        }

        public static Tuple<bool, string> TestGetStatesResults(this IEnumerable<State> results)
        {
            if (results == null)
                return Tuple.Create(false, "\t unexpected null returned");

            if (results.Count() != 2)
                return Tuple.Create(false, "\tWrong number of rows returned");

            var state = results.First();
            if (state.Name != "New York" || state.Abbreviation != "NY")
                return Tuple.Create(false, "\tStates returned in wrong order");

            if (state.Cities == null)
                return Tuple.Create(false, "\tCities property was not assigned for New York");

            if (state.Cities.Count() != 2)
                return Tuple.Create(false, "\tWrong number of cities were returned for New York");

            var city = state.Cities.First();
            if (city.Name != "New York" || city.Id != 0 || city.StateId != state.Id)
                return Tuple.Create(false, "Cities returned in wrong order");

            city = state.Cities.Last();
            if (city.Name != "Albany" || city.Id != 1 || city.StateId != state.Id)
                return Tuple.Create(false, "Cities returned in wrong order");

            state = results.Last();
            if (state.Name != "California" || state.Abbreviation != "CA")
                return Tuple.Create(false, "\tStates returned in wrong order");

            if (state.Cities == null)
                return Tuple.Create(false, "\tCities property was not assigned for California");

            if (state.Cities.Count() != 2)
                return Tuple.Create(false, "\tWrong number of cities were returned for California");

            city = state.Cities.First();
            if (city.Name != "San Francisco" || city.Id != 2 || city.StateId != state.Id)
                return Tuple.Create(false, "\tCities returned in wrong order");

            city = state.Cities.Last();
            if (city.Name != "Los Angeles" || city.Id != 3 || city.StateId != state.Id)
                return Tuple.Create(false, "\tCities returned in wrong order");

            return Tuple.Create(true, "");
        }
        
        public static Tuple<bool, string> TestGetWidgetResults(this Tuple<IEnumerable<Widget>, IEnumerable<WidgetComponent>> items)
        {
            var widget = items.Item1;
            var components = items.Item2;

            if (!widget.Any())
                return Tuple.Create(false, "\tDid not return any items in the first result set.");
            if (!components.Any())
                return Tuple.Create(false, "\tDid not return any items in the second result set.");
            if (widget.Count() > 1)
                return Tuple.Create(false, "\tUnexpected results returned in the first result set.");

            var w = widget.Single();
            if (w.WidgetId != 1 ||
               !w.IsNew.HasValue ||
                w.IsNew.Value ||
                w.Name != "Grub" ||
                w.Price != 22.22M ||
                w.Weight != 3.3)
            {
                return Tuple.Create(false, "\tData not mapped correctly in first result set.");
            }

            if (components.Count() != 3)
                return Tuple.Create(false, "\tUnexpected results returned in the second result set.");

            var ids = components.Select(c => c.WidgetComponentId)
                                .OrderBy(i => i);

            if (!ids.SequenceEqual(new[] { 2, 3, 4 }))
                return Tuple.Create(false, "\tData not mapped correctly in second result set.");

            var names = components.Select(c => c.Name)
                                  .OrderBy(s => s);

            if (!names.SequenceEqual(new[] { "Antennae", "Compound Eye", "Leg" }))
                return Tuple.Create(false, "\tData not mapped correctly in second result set.");

            return Tuple.Create(true, "");
        }

        public static Tuple<bool, string> TestGetExistingPeopleResults(this IEnumerable<Person> res)
        {
            if (res == null)
                return Tuple.Create(false, "\tNull returned");

            if (res.Count() != 2)
                return Tuple.Create(false, "\tWrong number of results returned");

            var item = res.First();
            if (item.FirstName != "John" || item.LastName != "Doe")
                return Tuple.Create(false, string.Format("\tIncorrect first item.\n\t\tExpected 'John Doe' \n\t\tActual '{0} {1}'", item.FirstName, item.LastName));

            item = res.Last();
            if (item.FirstName != "Jane" || item.LastName != "Doe")
                return Tuple.Create(false, string.Format("\tIncorrect last item.\n\t\tExpected 'Jane Doe' \n\t\tActual '{0} {1}'", item.FirstName, item.LastName));

            return Tuple.Create(true, "");
        }

        public static Tuple<bool, string> TestEmptyPeopleResults(this IEnumerable<Person> res)
        {
            if (res == null)
                return Tuple.Create(false, "\tNull returned");

            if (res.Any())
                return Tuple.Create(false, "\tWrong number of results returned");

            return Tuple.Create(true, "");
        }

        public static Tuple<bool, string> TestGetWidgetResultsDynamic(this Tuple<IEnumerable<dynamic>, IEnumerable<dynamic>> items)
        {
            var widget = items.Item1;
            var components = items.Item2;

            if (!widget.Any())
                return Tuple.Create(false, "\tDid not return any items in the first result set.");
            if (!components.Any())
                return Tuple.Create(false, "\tDid not return any items in the second result set.");

            if (widget.Count() > 1)
                return Tuple.Create(false, "\tUnexpected results returned in the first result set.");

            var w = widget.Single();
            if (w.WidgetId != 1 ||
                w.IsNew ||
                w.Name != "Grub" ||
                w.Price != 22.22M ||
                w.Weight != 3.3)
            {
                return Tuple.Create(false, "\tData not mapped correctly in first result set.");
            }

            if (components.Count() != 3)
                return Tuple.Create(false, "\tUnexpected results returned in the second result set.");

            var ids = components.Select(c => (int)c.WidgetComponentId)
                                .OrderBy(i => i);

            if (!ids.SequenceEqual(new[] { 2, 3, 4 }))
                return Tuple.Create(false, "\tData not mapped correctly in second result set.");

            var names = components.Select(c => c.Name)
                                  .OrderBy(s => s);

            if (!names.SequenceEqual(new[] { "Antennae", "Compound Eye", "Leg" }))
                return Tuple.Create(false, "\tData not mapped correctly in second result set.");

            return Tuple.Create(true, "");
        }
    }
}
