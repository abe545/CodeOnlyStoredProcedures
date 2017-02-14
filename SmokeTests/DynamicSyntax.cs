using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.SqlClient;
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

        [SmokeTest("Dynamic Syntax Single ResultSet with custom struct Parameter")]
        Tuple<bool, string> ExecuteSyncWithCustomStructParameter(IDbConnection db)
        {
            IEnumerable<Item> res = db.Execute(Program.timeout).usp_GetItem(new ItemIdInput { itemId = 0 });
            return res.TestGetItemResults();
        }

        [SmokeTest("Dynamic Syntax Single ResultSet with custom struct Parameter (Await)")]
        async Task<Tuple<bool, string>> ExecuteAsyncAwaitWithCustomStructParameter(IDbConnection db)
        {
            IEnumerable<Item> res = await db.ExecuteAsync(Program.timeout).usp_GetItem(new ItemIdInput { itemId = 0 });
            return res.TestGetItemResults();
        }

        [SmokeTest("Dynamic Syntax Single ResultSet with custom struct Parameter (Task)")]
        Task<Tuple<bool, string>> ExecuteAsyncWithCustomStructParameter(IDbConnection db)
        {
            Task<IEnumerable<Item>> res = db.ExecuteAsync(Program.timeout).usp_GetItem(new ItemIdInput { itemId = 0 });
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

        [SmokeTest("Dynamic Syntax DateTimeOffset parameter and result")]
        Tuple<bool, string> DateTimeOffset_Result_And_Parameter(IDbConnection db)
        {
            DateTimeOffset res, dt = DateTimeOffset.Now;
            res = db.Execute(Program.timeout).usp_GetAsUtc(dateTime: dt);

            if (res != dt.ToUniversalTime())
                return Tuple.Create(false, $"\texpected {dt.ToUniversalTime()}, but returned {res}");

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

        [SmokeTest("Dynamic Syntax async DateTimeOffset parameter and result")]
        async Task<Tuple<bool, string>> AsyncDateTimeOffset_Result_And_Parameter(IDbConnection db)
        {
            DateTimeOffset res, dt = DateTimeOffset.Now;
            res = await db.ExecuteAsync(Program.timeout).usp_GetAsUtc(dateTime: dt);

            if (res != dt.ToUniversalTime())
                return Tuple.Create(false, $"\texpected {dt.ToUniversalTime()}, but returned {res}");

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
            db.ExecuteNonQuery(Program.timeout).usp_ReturnsOne(returnValue: out retVal);

            if (retVal != 1)
                return Tuple.Create(false, "ReturnValue not set");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax ReturnValue via property on input class")]
        Tuple<bool, string> GetReturnValueWithInputProperty(IDbConnection db)
        {
            var input = new ReturnsOne();
            db.ExecuteNonQuery(Program.timeout).usp_ReturnsOne(input);

            if (input.ReturnValue != 1)
                return Tuple.Create(false, "ReturnValue not set");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax ReturnValue via property on input class (Await)")]
        async Task<Tuple<bool, string>> AsyncAwaitGetReturnValueWithInputProperty(IDbConnection db)
        {
            var input = new ReturnsOne();
            await db.ExecuteNonQueryAsync(Program.timeout).usp_ReturnsOne(input);

            if (input.ReturnValue != 1)
                return Tuple.Create(false, "ReturnValue not set");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax ReturnValue via property on input class (Task)")]
        Task<Tuple<bool, string>> AsyncTaskGetReturnValueWithInputProperty(IDbConnection db)
        {
            var input = new ReturnsOne();
            Task t = db.ExecuteNonQueryAsync(Program.timeout).usp_ReturnsOne(input);

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
        Person[] etvp = new Person[0];
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

        [SmokeTest("Dynamic Syntax empty TVP WithTableValuedParameter")]
        Tuple<bool, string> EmptyTVP_WithParameter(IDbConnection db)
        {
            return ((IEnumerable<Person>)db.Execute(Program.timeout).usp_GetExistingPeople(people: etvp))
                .TestEmptyPeopleResults();
        }

        [SmokeTest("Dynamic Syntax async empty TVP WithTableValuedParameter")]
        async Task<Tuple<bool, string>> EmptyAsyncTVP_WithParameter(IDbConnection db)
        {
            IEnumerable<Person> res = await db.ExecuteAsync(Program.timeout).usp_GetExistingPeople(people: etvp);
            return res.TestEmptyPeopleResults();
        }

        [SmokeTest("Dynamic Syntax empty TVP WithInput no Attribute")]
        Tuple<bool, string> EmptyTVP_WithInput_NoAttribute(IDbConnection db)
        {
            return ((IEnumerable<Person>)db.Execute(Program.timeout).usp_GetExistingPeople(new { people = etvp }))
                .TestEmptyPeopleResults();
        }

        [SmokeTest("Dynamic Syntax async empty TVP WithInput no Attribute")]
        async Task<Tuple<bool, string>> AsyncEmptyTVP_WithInput_NoAttribute(IDbConnection db)
        {
            IEnumerable<Person> res = await db.ExecuteAsync(Program.timeout).usp_GetExistingPeople(new { people = etvp });
            return res.TestEmptyPeopleResults();
        }

        [SmokeTest("Dynamic Syntax empty TVP WithInput with Attribute")]
        Tuple<bool, string> EmptyTVP_WithInput_WithAttribute(IDbConnection db)
        {
            return ((IEnumerable<Person>)db.Execute(Program.timeout).usp_GetExistingPeople(new PersonInput { People = etvp }))
                .TestEmptyPeopleResults();
        }

        [SmokeTest("Dynamic Syntax async empty TVP WithInput with Attribute")]
        async Task<Tuple<bool, string>> AsyncEmptyTVP_WithInput_WithAttribute(IDbConnection db)
        {
            IEnumerable<Person> res = await db.ExecuteAsync(Program.timeout).usp_GetExistingPeople(new PersonInput { People = etvp });
            return res.TestEmptyPeopleResults();
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

        #region Single Column Single Row TimeSpan
        [SmokeTest("Dynamic Syntax Single Column Single Row")]
        Tuple<bool, string> SingleColumnSingleRowTimeSpanSync(IDbConnection db)
        {
            var d1 = DateTime.Now;
            var d2 = d1.AddHours(1);
            TimeSpan result = db.Execute(Program.timeout).usp_TimeDifference(date1: d1, date2: d2);

            if (result != TimeSpan.FromHours(1))
                return Tuple.Create(false, string.Format("expected {0}, but returned {1}", TimeSpan.FromHours(1), result));

            return Tuple.Create(true, "");
        }

        // THIS DOESN'T WORK... Not sure if it can ever be enabled. Maybe if null parameters aren't actually passed to the sproc?
        //[SmokeTest("Dynamic Syntax Single Column Single Row with nullable parameter")]
        Tuple<bool, string> SingleColumnSingleRowTimeSpanSyncWithNullableParameter(IDbConnection db)
        {
            var d1 = DateTime.Now.AddHours(-1);
            DateTime? d2 = null;
            TimeSpan result = db.Execute(Program.timeout).usp_TimeDifference(date1: d1, date2: d2);

            if ((result - System.TimeSpan.FromHours(1)).Duration() > TimeSpan.FromSeconds(1))
                return Tuple.Create(false, string.Format("expected value at least {0}, but returned {1}", TimeSpan.FromHours(1), result));

            return Tuple.Create(true, "");
        }
        
        [SmokeTest("Dynamic Syntax Single Column Single Row with nullable parameter via anonymous type")]
        Tuple<bool, string> SingleColumnSingleRowTimeSpanSyncWithNullableParameterViaAnonymousType(IDbConnection db)
        {
            var d1 = DateTime.Now.AddHours(-1);
            DateTime? d2 = null;
            TimeSpan result = db.Execute(Program.timeout).usp_TimeDifference(new
            {
                date1 = d1,
                date2 = d2
            });

            if ((result - System.TimeSpan.FromHours(1)).Duration() > TimeSpan.FromSeconds(1))
                return Tuple.Create(false, string.Format("expected value at least {0}, but returned {1}", TimeSpan.FromHours(1), result));

            return Tuple.Create(true, "");
        }
        
        [SmokeTest("Dynamic Syntax Single Column Single Row (Await)")]
        async Task<Tuple<bool, string>> SingleColumnSingleRowTimeSpanAsync(IDbConnection db)
        {
            var d1 = DateTime.Now;
            var d2 = d1.AddHours(1);
            TimeSpan result = await db.ExecuteAsync(Program.timeout).usp_TimeDifference(date1: d1, date2: d2);

            if (result != TimeSpan.FromHours(1))
                return Tuple.Create(false, string.Format("expected {0}, but returned {1}", TimeSpan.FromHours(1), result));

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax Single Column Single Row (Task)")]
        Task<Tuple<bool, string>> SingleColumnSingleRowTimeSpanTask(IDbConnection db)
        {

            var d1 = DateTime.Now;
            var d2 = d1.AddHours(1);
            Task<TimeSpan> result = db.ExecuteAsync(Program.timeout).usp_TimeDifference(date1: d1, date2: d2);

            return result.ContinueWith(r =>
            {
                if (r.Result != TimeSpan.FromHours(1))
                    return Tuple.Create(false, string.Format("expected {0}, but returned {1}", TimeSpan.FromHours(1), r.Result));

                return Tuple.Create(true, "");
            });
        }

        [SmokeTest("Dynamic Syntax Single Column Single Row Untyped")]
        Tuple<bool, string> SingleColumnSingleRowUntypedTimeSpanSync(IDbConnection db)
        {
            var d1 = DateTime.Now;
            var d2 = d1.AddHours(1);
            TimeSpan result = db.Execute(Program.timeout, new TimeDoubler()).usp_TimeDifference(date1: d1, date2: d2);

            if (result != TimeSpan.FromHours(2))
                return Tuple.Create(false, string.Format("expected {0}, but returned {1}", TimeSpan.FromHours(2), result));

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax Single Column Single Row Untyped (Await)")]
        async Task<Tuple<bool, string>> SingleColumnSingleRowUntypedTimeSpanAsync(IDbConnection db)
        {
            var d1 = DateTime.Now;
            var d2 = d1.AddHours(1);
            TimeSpan result = await db.ExecuteAsync(Program.timeout, new TimeDoubler()).usp_TimeDifference(date1: d1, date2: d2);

            if (result != TimeSpan.FromHours(2))
                return Tuple.Create(false, string.Format("expected {0}, but returned {1}", TimeSpan.FromHours(2), result));

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax Single Column Single Row Untyped (Task)")]
        Task<Tuple<bool, string>> SingleColumnSingleRowUntypedTimeSpanTask(IDbConnection db)
        {

            var d1 = DateTime.Now;
            var d2 = d1.AddHours(1);
            Task<TimeSpan> result = db.ExecuteAsync(Program.timeout, new TimeDoubler()).usp_TimeDifference(date1: d1, date2: d2);

            return result.ContinueWith(r =>
            {
                if (r.Result != TimeSpan.FromHours(2))
                    return Tuple.Create(false, string.Format("expected {0}, but returned {1}", TimeSpan.FromHours(2), r.Result));

                return Tuple.Create(true, "");
            });
        }

        // don't implement the typed version, because we are trying to test the untyped retrieval with this transformer
        private class TimeDoubler : IDataTransformer
        {
            public bool CanTransform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
            {
                return targetType == typeof(TimeSpan);
            }

            public object Transform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
            {
                if (targetType != typeof(TimeSpan))
                    throw new NotSupportedException();

                var ts = (TimeSpan)value;
                return ts + ts;
            }
        }
        #endregion

        #region Non-existant Stored Procedure
        [SmokeTest("Dynamic Syntax Calling a non-existant stored procedure synchronously, expecitng results")]
        Tuple<bool, string> NonExistant_WithResults_Synchronously(IDbConnection db)
        {
            try
            {
                IEnumerable<Item> res = db.Execute(Program.timeout).usp_DoUknownStoredProcedure();
                return Tuple.Create(false, "Expected exception to be thrown, because the stored procedure doesn't exist, but none was.");
            }
            catch (SqlException)
            {
                return Tuple.Create(true, "");
            }
        }

        [SmokeTest("Dynamic Syntax Calling a non-existant stored procedure synchronously, expecitng no results")]
        Tuple<bool, string> NonExistant_WithoutResults_Synchronously(IDbConnection db)
        {
            try
            {
                db.Execute(Program.timeout).usp_DoUknownStoredProcedure();
                return Tuple.Create(false, "Expected exception to be thrown, because the stored procedure doesn't exist, but none was.");
            }
            catch (SqlException)
            {
                return Tuple.Create(true, "");
            }
        }

        [SmokeTest("Dynamic Syntax Calling a non-existant stored procedure asynchronously, expecitng results (Task)")]
        Task<Tuple<bool, string>> NonExistant_WithResults_Asynchronously(IDbConnection db)
        {
            Task<IEnumerable<Item>> res = db.ExecuteAsync(Program.timeout).usp_DoUknownStoredProcedure();
            return res.ContinueWith(r =>
            {
                if (r.Exception == null)
                    return Tuple.Create(false, "Expected exception to be thrown, because the stored procedure doesn't exist, but none was.");

                return Tuple.Create(true, "");
            });
        }

        [SmokeTest("Dynamic Syntax Calling a non-existant stored procedure asynchronously, expecitng no results (Task)")]
        Task<Tuple<bool, string>> NonExistant_WithoutResults_Asynchronously(IDbConnection db)
        {
            Task res = db.ExecuteAsync(Program.timeout).usp_DoUknownStoredProcedure();
            return res.ContinueWith(r =>
            {
                if (r.Exception == null)
                    return Tuple.Create(false, "Expected exception to be thrown, because the stored procedure doesn't exist, but none was.");

                return Tuple.Create(true, "");
            });
        }

        [SmokeTest("Dynamic Syntax Calling a non-existant stored procedure asynchronously, expecitng results (Await)")]
        async Task<Tuple<bool, string>> NonExistant_WithResults_Await_Asynchronously(IDbConnection db)
        {
            try
            {
                IEnumerable<Item> res = await db.ExecuteAsync(Program.timeout).usp_DoUknownStoredProcedure();
                return Tuple.Create(false, "Expected exception to be thrown, because the stored procedure doesn't exist, but none was.");
            }
            catch (AggregateException)
            {
                return Tuple.Create(true, "");
            }
        }

        [SmokeTest("Dynamic Syntax Calling a non-existant stored procedure asynchronously, expecitng no results (Await)")]
        async Task<Tuple<bool, string>> NonExistant_WithoutResults_Await_Asynchronously(IDbConnection db)
        {
            try
            {
                await db.ExecuteAsync(Program.timeout).usp_DoUknownStoredProcedure();
                return Tuple.Create(false, "Expected exception to be thrown, because the stored procedure doesn't exist, but none was.");
            }
            catch (AggregateException)
            {
                return Tuple.Create(true, "");
            }
        }
        #endregion

        #region Binary
        [SmokeTest("Dynamic Syntax Binary ResultSet")]
        Tuple<bool, string> ExecuteBinarySync(IDbConnection db)
        {
            byte[] res = db.Execute(Program.timeout).usp_GetIntAsBytes(@toBytes: 42);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(res);

            if (BitConverter.ToInt32(res, 0) != 42)
                return Tuple.Create(false, "The bytes returned from the stored procedure did not match the expected results");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax Empty Binary ResultSet")]
        Tuple<bool, string> ExecutEmptyBinarySync(IDbConnection db)
        {
            byte[] res = db.Execute(Program.timeout).usp_GetEmptyBinary(@includeResults: false);
            if (res != null)
                return Tuple.Create(false, "The stored procedure returned an empty result set, so should not have a value");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax NULL Binary ResultSet")]
        Tuple<bool, string> ExecutNullBinarySync(IDbConnection db)
        {
            byte[] res = db.Execute(Program.timeout).usp_GetEmptyBinary(@includeResults: true);
            if (res != null)
                return Tuple.Create(false, "The stored procedure returned an empty result set, so should not have a value");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax Binary ResultSet (await)")]
        async Task<Tuple<bool, string>> ExecuteBinaryAsync(IDbConnection db)
        {
            byte[] res = await db.ExecuteAsync(Program.timeout).usp_GetIntAsBytes(@toBytes: 42);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(res);

            if (BitConverter.ToInt32(res, 0) != 42)
                return Tuple.Create(false, "The bytes returned from the stored procedure did not match the expected results");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax Empty Binary ResultSet (await)")]
        async Task<Tuple<bool, string>> ExecutEmptyBinaryAsync(IDbConnection db)
        {
            byte[] res = await db.ExecuteAsync(Program.timeout).usp_GetEmptyBinary(@includeResults: false);
            if (res != null)
                return Tuple.Create(false, "The stored procedure returned an empty result set, so should not have a value");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax NULL Binary ResultSet (await)")]
        async Task<Tuple<bool, string>> ExecutNullBinaryAsync(IDbConnection db)
        {
            byte[] res = await db.ExecuteAsync(Program.timeout).usp_GetEmptyBinary(@includeResults: true);
            if (res != null)
                return Tuple.Create(false, "The stored procedure returned an empty result set, so should not have a value");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax Binary ResultSet (task)")]
        Task<Tuple<bool, string>> ExecuteBinaryTask(IDbConnection db)
        {
            Task<byte[]> t = db.ExecuteAsync(Program.timeout).usp_GetIntAsBytes(@toBytes: 42);

            return t.ContinueWith(r =>
            {
                var res = r.Result;

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(res);

                if (BitConverter.ToInt32(res, 0) != 42)
                    return Tuple.Create(false, "The bytes returned from the stored procedure did not match the expected results");

                return Tuple.Create(true, "");

            });
        }

        [SmokeTest("Dynamic Syntax Empty Binary ResultSet (task)")]
        Task<Tuple<bool, string>> ExecutEmptyBinaryTask(IDbConnection db)
        {
            Task<byte[]> t = db.ExecuteAsync(Program.timeout).usp_GetEmptyBinary(@includeResults: false);

            return t.ContinueWith(r =>
            {
                var res = t.Result;
                if (res != null)
                    return Tuple.Create(false, "The stored procedure returned an empty result set, so should not have a value");

                return Tuple.Create(true, "");
            });
        }

        [SmokeTest("Dynamic Syntax NULL Binary ResultSet (task)")]
        Task<Tuple<bool, string>> ExecutNullBinaryTask(IDbConnection db)
        {
            Task<byte[]> t = db.ExecuteAsync(Program.timeout).usp_GetEmptyBinary(@includeResults: true);

            return t.ContinueWith(r =>
            {
                var res = t.Result;
                if (res != null)
                    return Tuple.Create(false, "The stored procedure returned an empty result set, so should not have a value");

                return Tuple.Create(true, "");
            });
        }

        [SmokeTest("Dynamic Syntax Binary Parameter")]
        Tuple<bool, string> ExecuteBinaryParameter(IDbConnection db)
        {
            byte[] bytes = BitConverter.GetBytes(123456);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            int res = db.Execute(Program.timeout).usp_GetBytesAsInt(@toInt: bytes);
            if (res != 123456)
                return Tuple.Create(false, "The int returned from the stored procedure did not match the expected results");

            return Tuple.Create(true, "");
        }
        #endregion

        #region Reference Parameters
        [SmokeTest("Dynamic Syntax Reference Parameter (ExecuteNonQuery)")]
        Tuple<bool, string> ExecuteNonQueryRefParameterSync(IDbConnection db)
        {
            int value = 2;
            db.ExecuteNonQuery(Program.timeout).usp_Square(@value: ref value);
            if (value != 4)
                return Tuple.Create(false, $"The value was not squared after the stored procedure completed. Expected 4, value is {value}.");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax Reference Parameter (Execute)")]
        Tuple<bool, string> ExecuteRefParameterSync(IDbConnection db)
        {
            int value = 2;
            db.Execute(Program.timeout).usp_Square(@value: ref value);
            if (value != 4)
                return Tuple.Create(false, $"The value was not squared after the stored procedure completed. Expected 4, value is {value}.");

            return Tuple.Create(true, "");
        }

        private class SquareInput
        {
            [StoredProcedureParameter(Direction = ParameterDirection.InputOutput)]
            public int value { get; set; }
        }

        [SmokeTest("Dynamic Syntax Reference Parameter (await ExecuteNonQueryAsync)")]
        async Task<Tuple<bool, string>> ExecuteNonQueryAsyncRefParameterAsync(IDbConnection db)
        {
            var p = new SquareInput { value = 4 };
            await db.ExecuteNonQueryAsync(Program.timeout).usp_Square(p);
            if (p.value != 16)
                return Tuple.Create(false, $"The value was not squared after the stored procedure completed. Expected 16, value is {p.value}.");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax Reference Parameter (task ExecuteNonQueryAsync)")]
        Task<Tuple<bool, string>> ExecuteNonQueryAsyncRefParameterTask(IDbConnection db)
        {
            var p = new SquareInput { value = 4 };
            Task t = db.ExecuteNonQueryAsync(Program.timeout).usp_Square(p);

            return t.ContinueWith(r =>
            {
                if (p.value != 16)
                    return Tuple.Create(false, $"The value was not squared after the stored procedure completed. Expected 16, value is {p.value}.");

                return Tuple.Create(true, "");

            });
        }

        [SmokeTest("Dynamic Syntax Reference Parameter (await ExecuteAsync)")]
        async Task<Tuple<bool, string>> ExecuteRefParameterAsync(IDbConnection db)
        {
            var p = new SquareInput { value = 4 };
            await db.ExecuteAsync(Program.timeout).usp_Square(p);
            if (p.value != 16)
                return Tuple.Create(false, $"The value was not squared after the stored procedure completed. Expected 16, value is {p.value}.");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax Reference Parameter (task ExecuteAsync)")]
        Task<Tuple<bool, string>> ExecuteRefParameterTask(IDbConnection db)
        {
            var p = new SquareInput { value = 4 };
            Task t = db.ExecuteAsync(Program.timeout).usp_Square(p);

            return t.ContinueWith(r =>
            {
                if (p.value != 16)
                    return Tuple.Create(false, $"The value was not squared after the stored procedure completed. Expected 16, value is {p.value}.");

                return Tuple.Create(true, "");

            });
        }

        [SmokeTest("Dynamic Syntax Reference Parameter with Results", ignore: true)]
        Tuple<bool, string> ExecuteRefParameterResultsSync(IDbConnection db)
        {
            int value = 18;
            IEnumerable<int> parameters = db.Execute(Program.timeout).usp_Add(@param1: 3, @param2: 4, @result: out value);

            if (value != 7)
                return Tuple.Create(false, $"The value was not set after the stored procedure completed. Expected 7, value is {value}.");
            if (!parameters.SequenceEqual(new[] { 3, 4 }))
                return Tuple.Create(false, $"Expected the input values to be returned from the stored procedure. Expected [3,4], received [{ string.Join(",", parameters.Select(i => i.ToString())) }]");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax Output Parameter with Results (via input class)")]
        Tuple<bool, string> ExecuteOutputParameterResultsSync(IDbConnection db)
        {
            var input = new AddInput { Param1 = 3, Param2 = 4 };
            IEnumerable<int> parameters = db.Execute(Program.timeout).usp_Add(input);

            if (input.Result != 7)
                return Tuple.Create(false, $"The value was not set after the stored procedure completed. Expected 7, value is {input.Result}.");
            if (!parameters.SequenceEqual(new[] { 3, 4 }))
                return Tuple.Create(false, $"Expected the input values to be returned from the stored procedure. Expected [3,4], received [{ string.Join(",", parameters.Select(i => i.ToString())) }]");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax Reference Parameter with Results (async)")]
        async Task<Tuple<bool, string>> ExecuteAsyncRefParameterResultsSync(IDbConnection db)
        {
            var input = new AddInput { Param1 = 3, Param2 = 4 };
            IEnumerable<int> parameters = await db.ExecuteAsync(Program.timeout).usp_Add(input);

            if (input.Result != 7)
                return Tuple.Create(false, $"The value was not set after the stored procedure completed. Expected 7, value is {input.Result}.");
            if (!parameters.SequenceEqual(new[] { 3, 4 }))
                return Tuple.Create(false, $"Expected the input values to be returned from the stored procedure. Expected [3,4], received [{ string.Join(",", parameters.Select(i => i.ToString())) }]");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Dynamic Syntax Output Parameter with Results (Task)")]
        Task<Tuple<bool, string>> ExecuteTaskOutputParameterResultsSync(IDbConnection db)
        {
            var input = new AddInput { Param1 = 3, Param2 = 4 };
            Task<IEnumerable<int>> parameters = db.ExecuteAsync(Program.timeout).usp_Add(input);

            return parameters.ContinueWith(r =>
            {
                if (input.Result != 7)
                    return Tuple.Create(false, $"The value was not set after the stored procedure completed. Expected 7, value is {input.Result}.");
                if (!r.Result.SequenceEqual(new[] { 3, 4 }))
                    return Tuple.Create(false, $"Expected the input values to be returned from the stored procedure. Expected [3,4], received [{ string.Join(",", r.Result.Select(i => i.ToString())) }]");

                return Tuple.Create(true, "");
            });
        }

        class AddInput
        {
            public int Param1 { get; set; }
            public int Param2 { get; set; }
            [StoredProcedureParameter(Direction = ParameterDirection.Output)]
            public int Result { get; set; }
        }
        #endregion
    }
}
