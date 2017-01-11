using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure;
using CodeOnlyStoredProcedure.DataTransformation;

namespace SmokeTests
{
    [Export]
    class FluentSyntax
    {
        #region Single ResultSet
        [SmokeTest("Fluent Syntax Single ResultSet")]
        Tuple<bool, string> SingleResultSet(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetItems")
                                  .WithResults<Item>()
                                  .Execute(db, Program.timeout)
                                  .TestGetItemsResults();
        }

        [SmokeTest("Fluent Syntax Single ResultSet with Missing Columns (should throw)")]
        Tuple<bool, string> SingleResultSet_WithMissingColumns(IDbConnection db)
        {
            try
            {
                StoredProcedure.Create("usp_GetItems")
                               .WithResults<ItemShouldThrow>()
                               .Execute(db, Program.timeout);

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

        [SmokeTest("Fluent Syntax Single ResultSet WithParameter")]
        Tuple<bool, string> SingleResultSet_WithParameter(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetItem")
                                  .WithResults<Item>()
                                  .WithParameter("ItemId", 0)
                                  .Execute(db, Program.timeout)
                                  .TestGetItemResults();
        }

        [SmokeTest("Fluent Syntax Single ResultSet WithParameter (expecting no results)")]
        Tuple<bool, string> SingleResultSet_WithParameterNoResults(IDbConnection db)
        {
            var res = StoredProcedure.Create("usp_GetItem")
                                     .WithResults<Item>()
                                     .WithParameter("ItemId", 41)
                                     .Execute(db, Program.timeout);

            if (res.Any())
                return Tuple.Create(false, "\t" + res.Count() + " items returned");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax Single ResultSet WithInput")]
        Tuple<bool, string> SingleResultSet_WithInput(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetItem")
                                  .WithResults<Item>()
                                  .WithInput(new { ItemId = 0 })
                                  .Execute(db, Program.timeout)
                                  .TestGetItemResults();
        }

        [SmokeTest("Fluent Syntax Single ResultSet WithIDataTransformer")]
        Tuple<bool, string> SingleResultSet_WithIDataTransformer(IDbConnection db)
        {
            var res = StoredProcedure.Create("usp_GetItems")
                                     .WithResults<Item>()
                                     .WithDataTransformer(new InternAllStringsTransformer())
                                     .Execute(db, Program.timeout);

            var test = res.TestGetItemsResults();
            if (!test.Item1)
                return test;

            // if any of the strings are not interned, the transformer didn't run
            if (res.Any(i => string.IsInterned(i.Name) == null))
                return Tuple.Create(false, "The IDataTransformer was not run on for all the rows");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax async Single ResultSet")]
        async Task<Tuple<bool, string>> AsyncSingleResultSet(IDbConnection db)
        {
            var res = await StoredProcedure.Create("usp_GetItems")
                                           .WithResults<Item>()
                                           .ExecuteAsync(db, Program.timeout);

            return res.TestGetItemsResults();
        }

        [SmokeTest("Fluent Syntax async Single ResultSet WithParameter")]
        async Task<Tuple<bool, string>> AsyncSingleResultSet_WithParameter(IDbConnection db)
        {
            var res = await StoredProcedure.Create("usp_GetItem")
                                           .WithResults<Item>()
                                           .WithParameter("ItemId", 0)
                                           .ExecuteAsync(db, Program.timeout);

            return res.TestGetItemResults();
        }

        [SmokeTest("Fluent Syntax async Single ResultSet WithInput")]
        async Task<Tuple<bool, string>> AsyncSingleResultSet_WithInput(IDbConnection db)
        {
            var res = await StoredProcedure.Create("usp_GetItem")
                                           .WithResults<Item>()
                                           .WithInput(new { ItemId = 0 })
                                           .ExecuteAsync(db, Program.timeout);

            return res.TestGetItemResults();
        }

        [SmokeTest("Fluent Syntax async Single ResultSet WithIDataTransformer")]
        async Task<Tuple<bool, string>> AsyncSingleResultSet_WithIDataTransformer(IDbConnection db)
        {
            var res = await StoredProcedure.Create("usp_GetItems")
                                           .WithResults<Item>()
                                           .WithDataTransformer(new InternAllStringsTransformer())
                                           .ExecuteAsync(db, Program.timeout);

            var test = res.TestGetItemsResults();
            if (!test.Item1)
                return test;

            // if any of the strings are not interned, the transformer didn't run
            if (res.Any(i => string.IsInterned(i.Name) == null))
                return Tuple.Create(false, "The IDataTransformer was not run on for all the rows");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax Task Single ResultSet multiple asynchronous calls")]
        Task<Tuple<bool, string>> TaskSingleResultSet_MultipleExecutionsConcurrently(IDbConnection db)
        {
            var res1 = StoredProcedure.Create("usp_GetItem")
                                      .WithResults<Item>()
                                      .WithParameter("ItemId", 0)
                                      .ExecuteAsync(db, Program.timeout);
            var res2 = StoredProcedure.Create("usp_GetItem")
                                      .WithResults<Item>()
                                      .WithParameter("ItemId", 1)
                                      .ExecuteAsync(db, Program.timeout);

            return Task.Factory.ContinueWhenAll(new Task[] { res1, res2 }, _ =>
            {
                var res = res1.Result.TestGetItemResults();
                if (!res.Item1)
                    return res;

                return res2.Result.TestGetItemResults(1, "Bar");
            });
        }
        #endregion

        #region Simple ResultSet
        [SmokeTest("Fluent Syntax Simple ResultSet")]
        Tuple<bool, string> SimpleResultSet(IDbConnection db)
        {
            var res = StoredProcedure.Create("usp_GetSpokes")
                                     .WithResults<int>()
                                     .Execute(db, Program.timeout);

            if (!res.SequenceEqual(new[] { 4, 8, 16 }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax Simple ResultSet WithParameter")]
        Tuple<bool, string> SimpleResultSet_WithParameter(IDbConnection db)
        {
            var res = StoredProcedure.Create("usp_GetSpokes")
                                     .WithResults<int>()
                                     .WithParameter("minimumSpokes", 9)
                                     .Execute(db, Program.timeout);

            if (!res.SequenceEqual(new[] { 16 }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax DateTimeOffset parameter and result")]
        Tuple<bool, string> DateTimeOffset_Result_And_Parameter(IDbConnection db)
        {
            var dt = DateTimeOffset.Now;
            var res = StoredProcedure.Create("usp_GetAsUtc")
                                     .WithResults<DateTimeOffset>()
                                     .WithParameter("dateTime", dt)
                                     .Execute(db, Program.timeout)
                                     .Single();

            if (res != dt.ToUniversalTime())
                return Tuple.Create(false, $"\texpected {dt.ToUniversalTime()}, but returned {res}");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax Simple ResultSet WithInput")]
        Tuple<bool, string> SimpleResultSet_WithInput(IDbConnection db)
        {
            var res = StoredProcedure.Create("usp_GetSpokes")
                                     .WithResults<int>()
                                     .WithInput(new { minimumSpokes = 6 })
                                     .Execute(db, Program.timeout);

            if (!res.SequenceEqual(new[] { 8, 16 }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax Simple ResultSet with IDataTransformer")]
        Tuple<bool, string> SimpleResultSet_WithDataTransformer(IDbConnection db)
        {
            var res = StoredProcedure.Create("usp_GetSpokes")
                                     .WithResults<int>()
                                     .WithDataTransformer(new DoublingTransformer())
                                     .Execute(db, Program.timeout);

            if (!res.SequenceEqual(new[] { 8, 16, 32 }))
                return Tuple.Create(false, "\ttransformer not run on all returned rows");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax async Simple ResultSet")]
        async Task<Tuple<bool, string>> AsyncSimpleResultSet(IDbConnection db)
        {
            var res = await StoredProcedure.Create("usp_GetSpokes")
                                           .WithResults<int>()
                                           .ExecuteAsync(db, Program.timeout);

            if (!res.SequenceEqual(new[] { 4, 8, 16 }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax async Simple ResultSet WithParameter")]
        async Task<Tuple<bool, string>> AsyncSimpleResultSet_WithParameter(IDbConnection db)
        {
            var res = await StoredProcedure.Create("usp_GetSpokes")
                                           .WithResults<int>()
                                           .WithParameter("minimumSpokes", 9)
                                           .ExecuteAsync(db, Program.timeout);

            if (!res.SequenceEqual(new[] { 16 }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax async DateTimeOffset parameter and result")]
        async Task<Tuple<bool, string>> AsyncDateTimeOffset_Result_And_Parameter(IDbConnection db)
        {
            var dt = DateTimeOffset.Now;
            var res = (await StoredProcedure.Create("usp_GetAsUtc")
                                            .WithResults<DateTimeOffset>()
                                            .WithParameter("dateTime", dt)
                                            .ExecuteAsync(db, Program.timeout))
                                            .Single();

            if (res != dt.ToUniversalTime())
                return Tuple.Create(false, $"\texpected {dt.ToUniversalTime()}, but returned {res}");

            return Tuple.Create(true, "");

            if (res != dt.ToUniversalTime())
                return Tuple.Create(false, $"\texpected {dt.ToUniversalTime()}, but returned {res}");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax async Simple ResultSet WithInput")]
        async Task<Tuple<bool, string>> AsyncSimpleResultSet_WithInput(IDbConnection db)
        {
            var res = await StoredProcedure.Create("usp_GetSpokes")
                                           .WithResults<int>()
                                           .WithInput(new { minimumSpokes = 6 })
                                           .ExecuteAsync(db, Program.timeout);

            if (!res.SequenceEqual(new[] { 8, 16 }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax async Simple ResultSet with IDataTransformer")]
        async Task<Tuple<bool, string>> AsyncSimpleResultSet_WithDataTransformer(IDbConnection db)
        {
            var res = await StoredProcedure.Create("usp_GetSpokes")
                                           .WithResults<int>()
                                           .WithDataTransformer(new DoublingTransformer())
                                           .ExecuteAsync(db, Program.timeout);

            if (!res.SequenceEqual(new[] { 8, 16, 32 }))
                return Tuple.Create(false, "\ttransformer not run on all returned rows");

            return Tuple.Create(true, "");
        }
        #endregion

        #region Enum ResultSet
        [SmokeTest("Fluent Syntax Enum ResultSet")]
        Tuple<bool, string> EnumResultSet(IDbConnection db)
        {
            var res = StoredProcedure.Create("usp_GetSpokes")
                                     .WithResults<Spoke>()
                                     .Execute(db, Program.timeout);

            if (!res.SequenceEqual(new[] { Spoke.Four, Spoke.Eight, Spoke.Sixteen }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax Enum ResultSet WithParameter")]
        Tuple<bool, string> EnumResultSet_WithParameter(IDbConnection db)
        {
            var res = StoredProcedure.Create("usp_GetSpokes")
                                     .WithResults<Spoke>()
                                     .WithParameter("minimumSpokes", 9)
                                     .Execute(db, Program.timeout);

            if (!res.SequenceEqual(new[] { Spoke.Sixteen }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax Enum ResultSet WithInput")]
        Tuple<bool, string> EnumResultSet_WithInput(IDbConnection db)
        {
            var res = StoredProcedure.Create("usp_GetSpokes")
                                     .WithResults<Spoke>()
                                     .WithInput(new { minimumSpokes = 6 })
                                     .Execute(db, Program.timeout);

            if (!res.SequenceEqual(new[] { Spoke.Eight, Spoke.Sixteen }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax Enum ResultSet with IDataTransformer")]
        Tuple<bool, string> EnumResultSet_WithDataTransformer(IDbConnection db)
        {
            var res = StoredProcedure.Create("usp_GetSpokes")
                                     .WithResults<Spoke>()
                                     .WithDataTransformer(new DoublingTransformer())
                                     .Execute(db, Program.timeout);

            if (!res.SequenceEqual(new[] { Spoke.Eight, Spoke.Sixteen, (Spoke)32 }))
                return Tuple.Create(false, "\ttransformer not run on all returned rows");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax async Enum ResultSet")]
        async Task<Tuple<bool, string>> AsyncEnumResultSet(IDbConnection db)
        {
            var res = await StoredProcedure.Create("usp_GetSpokes")
                                           .WithResults<Spoke>()
                                           .ExecuteAsync(db, Program.timeout);

            if (!res.SequenceEqual(new[] { Spoke.Four, Spoke.Eight, Spoke.Sixteen }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax async Enum ResultSet WithParameter")]
        async Task<Tuple<bool, string>> AsyncEnumResultSet_WithParameter(IDbConnection db)
        {
            var res = await StoredProcedure.Create("usp_GetSpokes")
                                           .WithResults<Spoke>()
                                           .WithParameter("minimumSpokes", 9)
                                           .ExecuteAsync(db, Program.timeout);

            if (!res.SequenceEqual(new[] { Spoke.Sixteen }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax async Enum ResultSet WithInput")]
        async Task<Tuple<bool, string>> AsyncEnumResultSet_WithInput(IDbConnection db)
        {
            var res = await StoredProcedure.Create("usp_GetSpokes")
                                           .WithResults<Spoke>()
                                           .WithInput(new { minimumSpokes = 6 })
                                           .ExecuteAsync(db, Program.timeout);

            if (!res.SequenceEqual(new[] { Spoke.Eight, Spoke.Sixteen }))
                return Tuple.Create(false, "\treturned the wrong data");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax async Enum ResultSet with IDataTransformer")]
        async Task<Tuple<bool, string>> AsyncEnumResultSet_WithDataTransformer(IDbConnection db)
        {
            var res = await StoredProcedure.Create("usp_GetSpokes")
                                           .WithResults<Spoke>()
                                           .WithDataTransformer(new DoublingTransformer())
                                           .ExecuteAsync(db, Program.timeout);

            if (!res.SequenceEqual(new[] { Spoke.Eight, Spoke.Sixteen, (Spoke)32 }))
                return Tuple.Create(false, "\ttransformer not run on all returned rows");

            return Tuple.Create(true, "");
        }
        #endregion

        #region Return Value
        [SmokeTest("Fluent Syntax return value via WithInput")]
        Tuple<bool, string> ReturnValueViaWithInput(IDbConnection db)
        {
            var ro = new ReturnsOne();
            StoredProcedure.Create("usp_ReturnsOne")
                           .WithInput(ro)
                           .Execute(db, Program.timeout);

            if (ro.ReturnValue != 1)
                return Tuple.Create(false, "Return value not set on input class");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax return value via WithReturnValue")]
        Tuple<bool, string> ReturnValueViaWithReturnValue(IDbConnection db)
        {
            int retVal = -1;
            StoredProcedure.Create("usp_ReturnsOne")
                           .WithReturnValue(r => retVal = r)
                           .Execute(db, Program.timeout);

            if (retVal != 1)
                return Tuple.Create(false, "Return value action not called");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax async return value via WithInput")]
        async Task<Tuple<bool, string>> AsyncReturnValueViaWithInput(IDbConnection db)
        {
            var ro = new ReturnsOne();
            await StoredProcedure.Create("usp_ReturnsOne")
                                 .WithInput(ro)
                                 .ExecuteAsync(db, Program.timeout);

            if (ro.ReturnValue != 1)
                return Tuple.Create(false, "Return value not set on input class");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax async return value via WithReturnValue")]
        async Task<Tuple<bool, string>> AsyncReturnValueViaWithReturnValue(IDbConnection db)
        {
            int retVal = -1;
            await StoredProcedure.Create("usp_ReturnsOne")
                                 .WithReturnValue(r => retVal = r)
                                 .ExecuteAsync(db, Program.timeout);

            if (retVal != 1)
                return Tuple.Create(false, "Return value action not called");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax multiple async return values via WithReturnValue")]
        Task<Tuple<bool, string>> MultipleAsyncReturnValuesSimultaneously(IDbConnection db)
        {
            var retVals = new ConcurrentBag<int>();
            var sp = StoredProcedure.Create("usp_ReturnsOne").WithReturnValue(retVals.Add);

            var t1 = sp.ExecuteAsync(db, Program.timeout);
            var t2 = sp.ExecuteAsync(db, Program.timeout);

            return Task.Factory.ContinueWhenAll(new Task[] { t1, t2 }, _ =>
            {
                if (retVals.Count != 2 || retVals.Any(i => i != 1))
                {
                    var err = new StringBuilder("\tBoth stored procedures did not return 1.");
                    int i = 0;
                    foreach (var b in retVals)
                    {
                        err.AppendFormat("\n\t\tResult {0} - {1}", i, b);
                        ++i;
                    }

                    return Tuple.Create(false, err.ToString());
                }

                return Tuple.Create(true, "");
            });
        }
        #endregion

        #region Multiple ResultSets
        [SmokeTest("Fluent Syntax Multiple ResultSets WithParameter")]
        Tuple<bool, string> MultipleResultSet_WithParameter(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetWidget")
                                  .WithResults<Widget, WidgetComponent>()
                                  .WithParameter("WidgetId", 1)
                                  .Execute(db, Program.timeout)
                                  .TestGetWidgetResults();
        }

        [SmokeTest("Fluent Syntax Multiple ResultSets WithInput")]
        Tuple<bool, string> MultipleResultSet_WithInput(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetWidget")
                                  .WithResults<Widget, WidgetComponent>()
                                  .WithInput(new { WidgetId = 1 })
                                  .Execute(db, Program.timeout)
                                  .TestGetWidgetResults();
        }

        [SmokeTest("Fluent Syntax async Multiple ResultSets WithParameter")]
        async Task<Tuple<bool, string>> AsyncMultipleResultSet_WithParameter(IDbConnection db)
        {
            var res = await StoredProcedure.Create("usp_GetWidget")
                                           .WithResults<Widget, WidgetComponent>()
                                           .WithParameter("WidgetId", 1)
                                           .ExecuteAsync(db, Program.timeout);
                                  
            return res.TestGetWidgetResults();
        }

        [SmokeTest("Fluent Syntax async Multiple ResultSets WithInput")]
        async Task<Tuple<bool, string>> AsyncMultipleResultSet_WithInput(IDbConnection db)
        {
            var res = await StoredProcedure.Create("usp_GetWidget")
                                           .WithResults<Widget, WidgetComponent>()
                                           .WithInput(new { WidgetId = 1 })
                                           .ExecuteAsync(db, Program.timeout);
                                  
            return res.TestGetWidgetResults();
        }

        [SmokeTest("Fluent Syntax two async Multiple ResultSets WithParameter simultaneously")]
        Task<Tuple<bool, string>> AsyncMultipleResultSet_MultipleExcecutions(IDbConnection db)
        {
            var sp = StoredProcedure.Create("usp_GetWidget")
                                    .WithResults<Widget, WidgetComponent>()
                                    .WithParameter("WidgetId", 1);
            var t1 = sp.ExecuteAsync(db, Program.timeout);
            var t2 = sp.ExecuteAsync(db, Program.timeout);

            return Task.Factory.ContinueWhenAll(new Task[] { t1, t2 }, _ =>
            {
                var res = t1.Result.TestGetWidgetResults();
                if (!res.Item1)
                    return res;

                return t2.Result.TestGetWidgetResults();
            });
        }
        #endregion

        #region Hierarchical ResultSet
        [SmokeTest("Fluent Syntax Hierarchical ResultSet when children returned first")]
        Tuple<bool, string> HierarchicalResultSet_ChildrenReturnedFirst(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetCitiesAndStates")
                                  .WithResults<State>()
                                  .Execute(db, Program.timeout)
                                  .TestGetStatesResults();
        }

        [SmokeTest("Fluent Syntax Hierarchical ResultSet when parents returned first")]
        Tuple<bool, string> HierarchicalResultSet_ParentsReturnedFirst(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetStatesAndCities")
                                  .WithResults<State>()
                                  .Execute(db, Program.timeout)
                                  .TestGetStatesResults();
        }

        [SmokeTest("Fluent Syntax Hierarchical ResultSet model order specified")]
        Tuple<bool, string> HierarchicalResultSet_ModelOrderSpecified(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetCitiesAndStates")
                                  .WithResults<City, State>()
                                  .AsHierarchical<State>()
                                  .Execute(db, Program.timeout)
                                  .TestGetStatesResults();
        }

        [SmokeTest("Fluent Syntax async Hierarchical ResultSet when children returned first")]
        async Task<Tuple<bool, string>> AsyncHierarchicalResultSet_ChildrenReturnedFirst(IDbConnection db)
        {
            var res = await StoredProcedure.Create("usp_GetCitiesAndStates")
                                           .WithResults<State>()
                                           .ExecuteAsync(db, Program.timeout);
                                  
            return res.TestGetStatesResults();
        }

        [SmokeTest("Fluent Syntax async Hierarchical ResultSet when parents returned first")]
        async Task<Tuple<bool, string>> AsyncHierarchicalResultSet_ParentsReturnedFirst(IDbConnection db)
        {
            var res = await StoredProcedure.Create("usp_GetStatesAndCities")
                                           .WithResults<State>()
                                           .ExecuteAsync(db, Program.timeout);

            return res.TestGetStatesResults();
        }

        [SmokeTest("Fluent Syntax Hierarchical ResultSet model order specified")]
        async Task<Tuple<bool, string>> AsyncHierarchicalResultSet_ModelOrderSpecified(IDbConnection db)
        {
            var res = await StoredProcedure.Create("usp_GetStatesAndCities")
                                           .WithResults<State, City>()
                                           .AsHierarchical<State>()
                                           .ExecuteAsync(db, Program.timeout);
                                  
            return res.TestGetStatesResults();
        }
        #endregion

        #region TableValuedParameter Input
        Person[] etvp = new Person[0];
        Person[] tvp = new[] 
        {
            new Person { FirstName = "John", LastName = "Doe" },
            new Person { FirstName = "Jane", LastName = "Doe" }
        };

        [SmokeTest("Fluent Syntax TVP WithTableValuedParameter")]
        Tuple<bool, string> TVP_WithTableValuedParameter(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetExistingPeople")
                                  .WithTableValuedParameter("people", tvp, "Person")
                                  .WithResults<Person>()
                                  .Execute(db, Program.timeout)
                                  .TestGetExistingPeopleResults();
        }

        [SmokeTest("Fluent Syntax async TVP WithTableValuedParameter")]
        Task<Tuple<bool, string>> AsyncTVP_WithTableValuedParameter(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetExistingPeople")
                                  .WithTableValuedParameter("people", tvp, "Person")
                                  .WithResults<Person>()
                                  .ExecuteAsync(db, Program.timeout)
                                  .ContinueWith(r => r.Result.TestGetExistingPeopleResults());
        }

        [SmokeTest("Fluent Syntax TVP WithInput no Attribute")]
        Tuple<bool, string> TVP_WithInput_NoAttribute(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetExistingPeople")
                                  .WithInput(new { people = tvp })
                                  .WithResults<Person>()
                                  .Execute(db, Program.timeout)
                                  .TestGetExistingPeopleResults();
        }

        [SmokeTest("Fluent Syntax async TVP WithInput no Attribute")]
        Task<Tuple<bool, string>> AsyncTVP_WithInput_NoAttribute(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetExistingPeople")
                                  .WithInput(new { people = tvp })
                                  .WithResults<Person>()
                                  .ExecuteAsync(db, Program.timeout)
                                  .ContinueWith(r => r.Result.TestGetExistingPeopleResults());
        }

        [SmokeTest("Fluent Syntax TVP WithInput with Attribute")]
        Tuple<bool, string> TVP_WithInput_WithAttribute(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetExistingPeople")
                                  .WithInput(new PersonInput { People = tvp })
                                  .WithResults<Person>()
                                  .Execute(db, Program.timeout)
                                  .TestGetExistingPeopleResults();
        }

        [SmokeTest("Fluent Syntax async TVP WithInput with Attribute")]
        Task<Tuple<bool, string>> AsyncTVP_WithInput_WithAttribute(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetExistingPeople")
                                  .WithInput(new PersonInput { People = tvp })
                                  .WithResults<Person>()
                                  .ExecuteAsync(db, Program.timeout)
                                  .ContinueWith(r => r.Result.TestGetExistingPeopleResults());
        }

        [SmokeTest("Fluent Syntax empty TVP WithTableValuedParameter")]
        Tuple<bool, string> EmptyTVP_WithTableValuedParameter(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetExistingPeople")
                                  .WithTableValuedParameter("people", etvp, "Person")
                                  .WithResults<Person>()
                                  .Execute(db, Program.timeout)
                                  .TestEmptyPeopleResults();
        }

        [SmokeTest("Fluent Syntax async empty TVP WithTableValuedParameter")]
        Task<Tuple<bool, string>> AsyncEmptyTVP_WithTableValuedParameter(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetExistingPeople")
                                  .WithTableValuedParameter("people", etvp, "Person")
                                  .WithResults<Person>()
                                  .ExecuteAsync(db, Program.timeout)
                                  .ContinueWith(r => r.Result.TestEmptyPeopleResults());
        }

        [SmokeTest("Fluent Syntax empty TVP WithInput no Attribute")]
        Tuple<bool, string> EmptyTVP_WithInput_NoAttribute(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetExistingPeople")
                                  .WithInput(new { people = etvp })
                                  .WithResults<Person>()
                                  .Execute(db, Program.timeout)
                                  .TestEmptyPeopleResults();
        }

        [SmokeTest("Fluent Syntax async empty TVP WithInput no Attribute")]
        Task<Tuple<bool, string>> AsyncEmptyTVP_WithInput_NoAttribute(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetExistingPeople")
                                  .WithInput(new { people = etvp })
                                  .WithResults<Person>()
                                  .ExecuteAsync(db, Program.timeout)
                                  .ContinueWith(r => r.Result.TestEmptyPeopleResults());
        }

        [SmokeTest("Fluent Syntax empty TVP WithInput with Attribute")]
        Tuple<bool, string> EmptyTVP_WithInput_WithAttribute(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetExistingPeople")
                                  .WithInput(new PersonInput { People = etvp })
                                  .WithResults<Person>()
                                  .Execute(db, Program.timeout)
                                  .TestEmptyPeopleResults();
        }

        [SmokeTest("Fluent Syntax async empty TVP WithInput with Attribute")]
        Task<Tuple<bool, string>> AsyncEmptyTVP_WithInput_WithAttribute(IDbConnection db)
        {
            return StoredProcedure.Create("usp_GetExistingPeople")
                                  .WithInput(new PersonInput { People = etvp })
                                  .WithResults<Person>()
                                  .ExecuteAsync(db, Program.timeout)
                                  .ContinueWith(r => r.Result.TestEmptyPeopleResults());
        }
        #endregion

        #region Single Column Single Row TimeSpan

        [SmokeTest("Fluent Syntax Single Column Single Row")]
        Tuple<bool, string> SingleColumnSingleRowTimeSpanSync(IDbConnection db)
        {
            var d1 = DateTime.Now;
            var d2 = d1.AddHours(1);
            var result = StoredProcedure.Create("usp_TimeDifference")
                                        .WithParameter("date1", d1)
                                        .WithParameter("date2", d2)
                                        .WithResults<TimeSpan>()
                                        .Execute(db, Program.timeout)
                                        .Single();

            if (result != TimeSpan.FromHours(1))
                return Tuple.Create(false, string.Format("expected {0}, but returned {1}", TimeSpan.FromHours(1), result));

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax Single Column Single Row (Await)")]
        async Task<Tuple<bool, string>> SingleColumnSingleRowTimeSpanAsync(IDbConnection db)
        {
            var d1 = DateTime.Now;
            var d2 = d1.AddHours(1);

            var results = await StoredProcedure.Create("usp_TimeDifference")
                                               .WithParameter("date1", d1)
                                               .WithParameter("date2", d2)
                                               .WithResults<TimeSpan>()
                                               .ExecuteAsync(db, Program.timeout);

            var result = results.Single();
            if (result != TimeSpan.FromHours(1))
                return Tuple.Create(false, string.Format("expected {0}, but returned {1}", TimeSpan.FromHours(1), result));

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax Single Column Single Row (Task)")]
        Task<Tuple<bool, string>> SingleColumnSingleRowTimeSpanTask(IDbConnection db)
        {

            var d1 = DateTime.Now;
            var d2 = d1.AddHours(1);

            var results = StoredProcedure.Create("usp_TimeDifference")
                                         .WithParameter("date1", d1)
                                         .WithParameter("date2", d2)
                                         .WithResults<TimeSpan>()
                                         .ExecuteAsync(db, Program.timeout);

            return results.ContinueWith(r =>
            {
                var res = r.Result.Single();
                if (res != TimeSpan.FromHours(1))
                    return Tuple.Create(false, string.Format("expected {0}, but returned {1}", TimeSpan.FromHours(1), res));

                return Tuple.Create(true, "");
            });
        }

        [SmokeTest("Fluent Syntax Single Column Single Row Untyped")]
        Tuple<bool, string> SingleColumnSingleRowUntypedTimeSpanSync(IDbConnection db)
        {
            var d1 = DateTime.Now;
            var d2 = d1.AddHours(1);
            var result = StoredProcedure.Create("usp_TimeDifference")
                                        .WithParameter("date1", d1)
                                        .WithParameter("date2", d2)
                                        .WithResults<TimespanResult>()
                                        .Execute(db, Program.timeout)
                                        .Single();

            if (result.Value != TimeSpan.FromHours(2))
                return Tuple.Create(false, string.Format("expected {0}, but returned {1}", TimeSpan.FromHours(2), result.Value));

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax Single Column Single Row Untyped (Await)")]
        async Task<Tuple<bool, string>> SingleColumnSingleRowUntypedTimeSpanAsync(IDbConnection db)
        {
            var d1 = DateTime.Now;
            var d2 = d1.AddHours(1);

            var results = await StoredProcedure.Create("usp_TimeDifference")
                                               .WithParameter("date1", d1)
                                               .WithParameter("date2", d2)
                                               .WithResults<TimespanResult>()
                                               .ExecuteAsync(db, Program.timeout);

            var result = results.Single();
            if (result.Value != TimeSpan.FromHours(2))
                return Tuple.Create(false, string.Format("expected {0}, but returned {1}", TimeSpan.FromHours(2), result.Value));

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax Single Column Single Row Untyped (Task)")]
        Task<Tuple<bool, string>> SingleColumnSingleRowUntypedTimeSpanTask(IDbConnection db)
        {

            var d1 = DateTime.Now;
            var d2 = d1.AddHours(1);

            var results = StoredProcedure.Create("usp_TimeDifference")
                                         .WithParameter("date1", d1)
                                         .WithParameter("date2", d2)
                                         .WithResults<TimespanResult>()
                                         .ExecuteAsync(db, Program.timeout);

            return results.ContinueWith(r =>
            {
                var res = r.Result.Single();
                if (res.Value != TimeSpan.FromHours(2))
                    return Tuple.Create(false, string.Format("expected {0}, but returned {1}", TimeSpan.FromHours(2), res.Value));

                return Tuple.Create(true, "");
            });
        }
        
        [SmokeTest("Fluent Syntax Single Column Single Row with nullable parameter (Task)")]
        Task<Tuple<bool, string>> SingleColumnSingleRowTimeSpanTaskWithNullableParameter(IDbConnection db)
        {
            var d1 = DateTime.Now.AddHours(-1);
            DateTime? d2 = null;

            var results = StoredProcedure.Create("usp_TimeDifference")
                                         .WithParameter("date1", d1)
                                         .WithParameter("date2", d2)
                                         .WithResults<TimeSpan>()
                                         .ExecuteAsync(db, Program.timeout);


            return results.ContinueWith(r =>
            {
                var res = r.Result.Single();
                if ((res - System.TimeSpan.FromHours(1)).Duration() > TimeSpan.FromSeconds(1))
                    return Tuple.Create(false, string.Format("expected {0}, but returned {1}", TimeSpan.FromHours(1), res));

                return Tuple.Create(true, "");
            });
        }

        [SmokeTest("Fluent Syntax Single Column Single Row with nullable parameter (Await)")]
        async Task<Tuple<bool, string>> SingleColumnSingleRowTimeSpanASyncWithNullableParameter(IDbConnection db)
        {
            var d1 = DateTime.Now.AddHours(-1);
            DateTime? d2 = null;

            var results = await StoredProcedure.Create("usp_TimeDifference")
                                               .WithParameter("date1", d1)
                                               .WithParameter("date2", d2)
                                               .WithResults<TimeSpan>()
                                               .ExecuteAsync(db, Program.timeout);

            var res = results.Single();
            if ((res - System.TimeSpan.FromHours(1)).Duration() > TimeSpan.FromSeconds(1))
                return Tuple.Create(false, string.Format("expected {0}, but returned {1}", TimeSpan.FromHours(1), res));

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax Single Column Single Row with nullable parameter (Await)")]
        Tuple<bool, string> SingleColumnSingleRowTimeSpanSyncWithNullableParameter(IDbConnection db)
        {
            var d1 = DateTime.Now.AddHours(-1);
            DateTime? d2 = null;

            var results = StoredProcedure.Create("usp_TimeDifference")
                                         .WithParameter("date1", d1)
                                         .WithParameter("date2", d2)
                                         .WithResults<TimeSpan>()
                                         .Execute(db, Program.timeout);

            var res = results.Single();
            if ((res - System.TimeSpan.FromHours(1)).Duration() > TimeSpan.FromSeconds(1))
                return Tuple.Create(false, string.Format("expected {0}, but returned {1}", TimeSpan.FromHours(1), res));

            return Tuple.Create(true, "");
        }

        private class TimespanResult
        {
            [DoubleTimespan]
            public TimeSpan Value { get; set; }
        }
        #endregion

        #region Binary Data
        [SmokeTest("Fluent Syntax Binary ResultSet")]
        Tuple<bool, string> ExecuteBinarySync(IDbConnection db)
        {
            byte[] res = StoredProcedure.Create ("usp_GetIntAsBytes")
                                        .WithParameter("toBytes", 42)
                                        .WithResults<byte[]>()
                                        .Execute(db, Program.timeout)
                                        .Single();

            if (BitConverter.IsLittleEndian)
                Array.Reverse(res);

            if (BitConverter.ToInt32(res, 0) != 42)
                return Tuple.Create(false, "The bytes returned from the stored procedure did not match the expected results");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax Binary ResultSet (await)")]
        async Task<Tuple<bool, string>> ExecuteBinaryAsync(IDbConnection db)
        {
            byte[] res = (await StoredProcedure.Create ("usp_GetIntAsBytes")
                                               .WithParameter("toBytes", 42)
                                               .WithResults<byte[]>()
                                               .ExecuteAsync(db, Program.timeout))
                                               .Single();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(res);

            if (BitConverter.ToInt32(res, 0) != 42)
                return Tuple.Create(false, "The bytes returned from the stored procedure did not match the expected results");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax Binary ResultSet (task)")]
        Task<Tuple<bool, string>> ExecuteBinaryTask(IDbConnection db)
        {
            var t = StoredProcedure.Create ("usp_GetIntAsBytes")
                                   .WithParameter("toBytes", 42)
                                   .WithResults<byte[]>()
                                   .ExecuteAsync(db, Program.timeout);

            return t.ContinueWith(r =>
            {
                var res = r.Result.Single();

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(res);

                if (BitConverter.ToInt32(res, 0) != 42)
                    return Tuple.Create(false, "The bytes returned from the stored procedure did not match the expected results");

                return Tuple.Create(true, "");

            });
        }

        [SmokeTest("Fluent Syntax Binary Parameter")]
        Tuple<bool, string> ExecuteBinaryParameter(IDbConnection db)
        {
            byte[] bytes = BitConverter.GetBytes(123456);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            int res = StoredProcedure.Create("usp_GetBytesAsInt")
                                     .WithParameter("toInt", bytes)
                                     .WithResults<int>()
                                     .Execute(db, Program.timeout)
                                     .Single();
            
            if (res != 123456)
                return Tuple.Create(false, "The int returned from the stored procedure did not match the expected results");

            return Tuple.Create(true, "");
        }
        #endregion

        #region Reference Parameters
        [SmokeTest("Fluent Syntax InputOutput Parameter")]
        Tuple<bool, string> ExecuteInputOutputParameterSync(IDbConnection db)
        {
            int value = 0;
            StoredProcedure.Create("usp_Square")
                           .WithInputOutputParameter("value", 4, x => value = x)
                           .Execute(db, Program.timeout);

            if (value != 16)
                return Tuple.Create(false, $"The value was not squared after the stored procedure completed. Expected 16, value is {value}.");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax InputOutput Property")]
        Tuple<bool, string> ExecuteInputOutputPropertySync(IDbConnection db)
        {
            var p = new SquareInput { value = 8 };
            StoredProcedure.Create("usp_Square")
                           .WithInput(p)
                           .Execute(db, Program.timeout);

            if (p.value != 64)
                return Tuple.Create(false, $"The value was not squared after the stored procedure completed. Expected 64, value is {p.value}.");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax InputOutput Parameter Async")]
        async Task<Tuple<bool, string>> ExecuteInputOutputParameterAsync(IDbConnection db)
        {
            int value = 0;
            await StoredProcedure.Create("usp_Square")
                                 .WithInputOutputParameter("value", 4, x => value = x)
                                 .ExecuteAsync(db, Program.timeout);

            if (value != 16)
                return Tuple.Create(false, $"The value was not squared after the stored procedure completed. Expected 16, value is {value}.");

            return Tuple.Create(true, "");
        }

        [SmokeTest("Fluent Syntax InputOutput Property Async")]
        async Task<Tuple<bool, string>> ExecuteInputOutputPropertyAsync(IDbConnection db)
        {
            var p = new SquareInput { value = 8 };
            await StoredProcedure.Create("usp_Square")
                                 .WithInput(p)
                                 .ExecuteAsync(db, Program.timeout);

            if (p.value != 64)
                return Tuple.Create(false, $"The value was not squared after the stored procedure completed. Expected 64, value is {p.value}.");

            return Tuple.Create(true, "");
        }

        private class SquareInput
        {
            [StoredProcedureParameter(Direction = ParameterDirection.InputOutput)]
            public int value { get; set; }
        }
        #endregion

        private class DoublingTransformer : IDataTransformer<int>, IDataTransformer<Spoke>
        {
            public bool CanTransform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
            {
                return targetType == typeof(int);
            }

            public object Transform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
            {
                return 2 * (int)value;
            }

            public int Transform(int value, IEnumerable<Attribute> propertyAttributes)
            {
                return value * 2;
            }

            public Spoke Transform(Spoke value, IEnumerable<Attribute> propertyAttributes)
            {
                return (Spoke)((int)value * 2);
            }
        }

        // This is untyped, because we need to test the untyped retrieval
        private class DoubleTimespanAttribute : DataTransformerAttributeBase
        {
            public override object Transform(object value, Type targetType, bool isNullable)
            {
                var ts = (TimeSpan)value;
                return ts + ts;
            }
        }

    }
}
