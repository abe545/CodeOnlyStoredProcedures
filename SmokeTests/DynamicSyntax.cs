using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure;

namespace SmokeTests
{
    [Export]
    class DynamicSyntax
    {
        #region Single ResultSet
        [SmokeTest("Dynamic Syntax Single ResultSet")]
        Tuple<bool, string> ExecuteSync(IDbConnection db)
        {
            IEnumerable<Item> res = db.Execute(Program.timeout).usp_GetItems();
            return res.TestGetItemsResults();
        }

        [SmokeTest("Dynamic Syntax Single ResultSet with Missing Columns (should throw)")]
        Tuple<bool, string> SingleResultSet_WithMissingColumns(IDbConnection db)
        {
            try
            {
                IEnumerable<ItemShouldThrow> res = db.Execute(Program.timeout).usp_GetItems();
                return Tuple.Create(false, "No exception was thrown, even though one of the expected columns is not mapped.");
            }
            catch (StoredProcedureResultsException)
            {
                return Tuple.Create(true, "");
            }
            catch (Exception ex)
            {
                return Tuple.Create(false, "Expected Exception of type StoredProcedureResultsException, but a " + ex.GetType() + " was thrown.\n" + ex.ToString());
            }
        }

        [SmokeTest("Dynamic Syntax Single ResultSet (Await)")]
        async Task<Tuple<bool, string>> ExecuteAsyncAwait(IDbConnection db)
        {
            IEnumerable<Item> res = await db.ExecuteAsync(Program.timeout).usp_GetItems();
            return res.TestGetItemsResults();
        }

        [SmokeTest("Dynamic Syntax Single ResultSet (Task)")]
        Task<Tuple<bool, string>> ExecuteAsync(IDbConnection db)
        {
            Task<IEnumerable<Item>> res = db.ExecuteAsync(Program.timeout).usp_GetItems();
            return res.ContinueWith(r => r.Result.TestGetItemsResults());
        }
        
        [SmokeTest("Dynamic Syntax Single ResultSet with Parameter")]
        Tuple<bool, string> ExecuteSyncWithParameter(IDbConnection db)
        {
            IEnumerable<Item> res = db.Execute(Program.timeout).usp_GetItem(ItemId: 0);
            return res.TestGetItemResults();
        }

        [SmokeTest("Dynamic Syntax Single ResultSet with Parameter (Await)")]
        async Task<Tuple<bool, string>> ExecuteAsyncAwaitWithParameter(IDbConnection db)
        {
            IEnumerable<Item> res = await db.ExecuteAsync(Program.timeout).usp_GetItem(ItemId: 0);
            return res.TestGetItemResults();
        }

        [SmokeTest("Dynamic Syntax Single ResultSet with Parameter (Task)")]
        Task<Tuple<bool, string>> ExecuteAsyncWithParameter(IDbConnection db)
        {
            Task<IEnumerable<Item>> res = db.ExecuteAsync(Program.timeout).usp_GetItem(ItemId: 0);
            return res.ContinueWith(r => r.Result.TestGetItemResults());
        }
        #endregion

        #region Simple ResultSet
        [SmokeTest("Dynamic Syntax Simple ResultSet")]
        Tuple<bool, string> SimpleResultSet(IDbConnection db)
        {
            IEnumerable<int> res = db.Execute(Program.timeout).usp_GetSpokes();

            if (!res.SequenceEqual(new[] { 4, 8, 16 }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax Simple ResultSet with parameter")]
        Tuple<bool, string> SimpleResultSet_WithParameter(IDbConnection db)
        {
            IEnumerable<int> res = db.Execute(Program.timeout).usp_GetSpokes(minimumSpokes: 9);

            if (!res.SequenceEqual(new[] { 16 }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax async Simple ResultSet")]
        async Task<Tuple<bool, string>> AsyncSimpleResultSet(IDbConnection db)
        {
            IEnumerable<int> res = await db.ExecuteAsync(Program.timeout).usp_GetSpokes();

            if (!res.SequenceEqual(new[] { 4, 8, 16 }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax async Simple ResultSet with parameter")]
        async Task<Tuple<bool, string>> AsyncSimpleResultSet_WithParameter(IDbConnection db)
        {
            IEnumerable<int> res = await db.ExecuteAsync(Program.timeout).usp_GetSpokes(minimumSpokes: 9);

            if (!res.SequenceEqual(new[] { 16 }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }
        #endregion

        #region Enum ResultSet
        [SmokeTest("Dynamic Syntax Enum ResultSet")]
        Tuple<bool, string> EnumResultSet(IDbConnection db)
        {
            IEnumerable<Spoke> res = db.Execute(Program.timeout).usp_GetSpokes();

            if (!res.SequenceEqual(new[] { Spoke.Four, Spoke.Eight, Spoke.Sixteen }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax Enum ResultSet WithParameter")]
        Tuple<bool, string> EnumResultSet_WithParameter(IDbConnection db)
        {
            IEnumerable<Spoke> res = db.Execute(Program.timeout).usp_GetSpokes(minimumSpokes: 9);

            if (!res.SequenceEqual(new[] { Spoke.Sixteen }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax async Enum ResultSet")]
        async Task<Tuple<bool, string>> AsyncEnumResultSet(IDbConnection db)
        {
            IEnumerable<Spoke> res = await db.ExecuteAsync(Program.timeout).usp_GetSpokes();

            if (!res.SequenceEqual(new[] { Spoke.Four, Spoke.Eight, Spoke.Sixteen }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax async Enum ResultSet WithParameter")]
        async Task<Tuple<bool, string>> AsyncEnumResultSet_WithParameter(IDbConnection db)
        {
            IEnumerable<Spoke> res = await db.ExecuteAsync(Program.timeout).usp_GetSpokes(minimumSpokes: 9);

            if (!res.SequenceEqual(new[] { Spoke.Sixteen }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }
        #endregion

        #region ReturnValue
        [SmokeTest("Dynamic Syntax ReturnValue via out parameter")]
        Tuple<bool, string> GetReturnValueWithOutParam(IDbConnection db)
        {
            int retVal = -1;
            db.Execute(Program.timeout).usp_ReturnsOne(returnValue: out retVal);

            if (retVal != 1)
                return Tuple.Create(false, "ReturnValue not set");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax ReturnValue via property on input class")]
        Tuple<bool, string> GetReturnValueWithInputProperty(IDbConnection db)
        {
            var input = new ReturnsOne();
            db.Execute(Program.timeout).usp_ReturnsOne(input);

            if (input.ReturnValue != 1)
                return Tuple.Create(false, "ReturnValue not set");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax ReturnValue via property on input class (Await)")]
        async Task<Tuple<bool, string>> AsyncAwaitGetReturnValueWithInputProperty(IDbConnection db)
        {
            var input = new ReturnsOne();
            await db.ExecuteAsync(Program.timeout).usp_ReturnsOne(input);

            if (input.ReturnValue != 1)
                return Tuple.Create(false, "ReturnValue not set");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax ReturnValue via property on input class (Task)")]
        Task<Tuple<bool, string>> AsyncTaskGetReturnValueWithInputProperty(IDbConnection db)
        {
            var input = new ReturnsOne();
            Task t = db.ExecuteAsync(Program.timeout).usp_ReturnsOne(input);

            return t.ContinueWith(_ =>
            {
                if (input.ReturnValue != 1)
                    return Tuple.Create(false, "ReturnValue not set");

                return Tuple.Create(true, "");
            });
        }
        #endregion

        #region Multiple ResultSets
        [SmokeTest("Dynamic Syntax Multiple ResultSets WithParameter")]
        Tuple<bool, string> MultipleResultSet_WithParameter(IDbConnection db)
        {
            Tuple<IEnumerable<Widget>, IEnumerable<WidgetComponent>> res = db.Execute(Program.timeout).usp_GetWidget(WidgetId: 1);                                  
            return res.TestGetWidgetResults();
        }

        [SmokeTest("Dynamic Syntax Multiple ResultSets WithInput")]
        Tuple<bool, string> MultipleResultSet_WithInput(IDbConnection db)
        {
            Tuple<IEnumerable<Widget>, IEnumerable<WidgetComponent>> res = db.Execute(Program.timeout).usp_GetWidget(new { WidgetId = 1 });
            return res.TestGetWidgetResults();
        }

        [SmokeTest("Dynamic Syntax async Multiple ResultSets WithParameter")]
        async Task<Tuple<bool, string>> AsyncMultipleResultSet_WithParameter(IDbConnection db)
        {
            Tuple<IEnumerable<Widget>, IEnumerable<WidgetComponent>> res = await db.ExecuteAsync(Program.timeout).usp_GetWidget(WidgetId: 1);
            return res.TestGetWidgetResults();
        }

        [SmokeTest("Dynamic Syntax async Multiple ResultSets WithInput")]
        async Task<Tuple<bool, string>> AsyncMultipleResultSet_WithInput(IDbConnection db)
        {
            Tuple<IEnumerable<Widget>, IEnumerable<WidgetComponent>> res = await db.ExecuteAsync(Program.timeout).usp_GetWidget(new { WidgetId = 1 });
            return res.TestGetWidgetResults();
        }

        [SmokeTest("Dynamic Syntax Task Multiple ResultSets WithParameter")]
        Task<Tuple<bool, string>> TaskMultipleResultSet_WithParameter(IDbConnection db)
        {
            Task<Tuple<IEnumerable<Widget>, IEnumerable<WidgetComponent>>> res = db.ExecuteAsync(Program.timeout).usp_GetWidget(WidgetId: 1);
            return res.ContinueWith(r => r.Result.TestGetWidgetResults());
        }

        [SmokeTest("Dynamic Syntax Task Multiple ResultSets WithInput")]
        Task<Tuple<bool, string>> TaskMultipleResultSet_WithInput(IDbConnection db)
        {
            Task<Tuple<IEnumerable<Widget>, IEnumerable<WidgetComponent>>> res = db.ExecuteAsync(Program.timeout).usp_GetWidget(new { WidgetId = 1 });
            return res.ContinueWith(r => r.Result.TestGetWidgetResults());
        }
        #endregion

        #region Hierarchical ResultSet
        [SmokeTest("Dynamic Syntax Hierarchical ResultSet when children returned first")]
        Tuple<bool, string> HierarchicalResultSet_ChildrenReturnedFirst(IDbConnection db)
        {
            IEnumerable<State> res = db.Execute(Program.timeout).usp_GetCitiesAndStates();
            return res.TestGetStatesResults();
        }

        [SmokeTest("Dynamic Syntax Hierarchical ResultSet when parents returned first")]
        Tuple<bool, string> HierarchicalResultSet_ParentsReturnedFirst(IDbConnection db)
        {
            IEnumerable<State> res = db.Execute(Program.timeout).usp_GetStatesAndCities();
            return res.TestGetStatesResults();
        }

        [SmokeTest("Dynamic Syntax async Hierarchical ResultSet when children returned first")]
        async Task<Tuple<bool, string>> AsyncHierarchicalResultSet_ChildrenReturnedFirst(IDbConnection db)
        {
            IEnumerable<State> res = await db.ExecuteAsync(Program.timeout).usp_GetCitiesAndStates();
            return res.TestGetStatesResults();
        }

        [SmokeTest("Dynamic Syntax async Hierarchical ResultSet when parents returned first")]
        async Task<Tuple<bool, string>> AsyncHierarchicalResultSet_ParentsReturnedFirst(IDbConnection db)
        {
            IEnumerable<State> res = await db.ExecuteAsync(Program.timeout).usp_GetStatesAndCities();
            return res.TestGetStatesResults();
        }

        [SmokeTest("Dynamic Syntax Task Hierarchical ResultSet when children returned first")]
        Task<Tuple<bool, string>> TaskHierarchicalResultSet_ChildrenReturnedFirst(IDbConnection db)
        {
            Task<IEnumerable<State>> res = db.ExecuteAsync(Program.timeout).usp_GetCitiesAndStates();
            return res.ContinueWith(r => r.Result.TestGetStatesResults());
        }

        [SmokeTest("Dynamic Syntax Task Hierarchical ResultSet when parents returned first")]
        Task<Tuple<bool, string>> TaskHierarchicalResultSet_ParentsReturnedFirst(IDbConnection db)
        {
            Task<IEnumerable<State>> res = db.ExecuteAsync(Program.timeout).usp_GetStatesAndCities();
            return res.ContinueWith(r => r.Result.TestGetStatesResults());
        }
        #endregion

        #region TableValuedParameter Input
        Person[] tvp = new[] 
        {
            new Person { FirstName = "John", LastName = "Doe" },
            new Person { FirstName = "Jane", LastName = "Doe" }
        };

        [SmokeTest("Dynamic Syntax TVP WithTableValuedParameter")]
        Tuple<bool, string> TVP_WithParameter(IDbConnection db)
        {
            return ((IEnumerable<Person>)db.Execute(Program.timeout).usp_GetExistingPeople(people: tvp))
                .TestGetExistingPeopleResults();
        }

        [SmokeTest("Dynamic Syntax async TVP WithTableValuedParameter")]
        async Task<Tuple<bool, string>> AsyncTVP_WithParameter(IDbConnection db)
        {
            IEnumerable<Person> res = await db.ExecuteAsync(Program.timeout).usp_GetExistingPeople(people: tvp);
            return res.TestGetExistingPeopleResults();
        }

        [SmokeTest("Dynamic Syntax TVP WithInput no Attribute")]
        Tuple<bool, string> TVP_WithInput_NoAttribute(IDbConnection db)
        {
            return ((IEnumerable<Person>)db.Execute(Program.timeout).usp_GetExistingPeople(new { people = tvp }))
                .TestGetExistingPeopleResults();
        }

        [SmokeTest("Dynamic Syntax async TVP WithInput no Attribute")]
        async Task<Tuple<bool, string>> AsyncTVP_WithInput_NoAttribute(IDbConnection db)
        {
            IEnumerable<Person> res = await db.ExecuteAsync(Program.timeout).usp_GetExistingPeople(new { people = tvp });
            return res.TestGetExistingPeopleResults();
        }

        [SmokeTest("Dynamic Syntax TVP WithInput with Attribute")]
        Tuple<bool, string> TVP_WithInput_WithAttribute(IDbConnection db)
        {
            return ((IEnumerable<Person>)db.Execute(Program.timeout).usp_GetExistingPeople(new PersonInput { People = tvp }))
                .TestGetExistingPeopleResults();
        }

        [SmokeTest("Dynamic Syntax async TVP WithInput with Attribute")]
        async Task<Tuple<bool, string>> AsyncTVP_WithInput_WithAttribute(IDbConnection db)
        {
            IEnumerable<Person> res = await db.ExecuteAsync(Program.timeout).usp_GetExistingPeople(new PersonInput { People = tvp });
            return res.TestGetExistingPeopleResults();
        }
        #endregion

        #region Dynamic ResultSet
        [SmokeTest("Dynamic Syntax Dynamic ResultSet")]
        Tuple<bool, string> DynamicResultSet(IDbConnection db)
        {
            Tuple<IEnumerable<dynamic>, IEnumerable<dynamic>> res = db.Execute(Program.timeout).usp_GetWidget(WidgetId: 1);
            return res.TestGetWidgetResultsDynamic();
        }

        [SmokeTest("Dynamic Syntax async Dynamic ResultSet")]
        async Task<Tuple<bool, string>> AsyncDynamicResultSet(IDbConnection db)
        {
            Tuple<IEnumerable<dynamic>, IEnumerable<dynamic>> res = await db.ExecuteAsync(Program.timeout).usp_GetWidget(WidgetId: 1);
            return res.TestGetWidgetResultsDynamic();
        }

        [SmokeTest("Dynamic Syntax Task Dynamic ResultSet")]
        Task<Tuple<bool, string>> TaskDynamicResultSet(IDbConnection db)
        {
            Task<Tuple<IEnumerable<dynamic>, IEnumerable<dynamic>>> res = db.ExecuteAsync(Program.timeout).usp_GetWidget(WidgetId: 1);
            return res.ContinueWith(r => r.Result.TestGetWidgetResultsDynamic());
        }
        #endregion
    }
}
