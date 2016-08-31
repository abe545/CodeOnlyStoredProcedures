using System;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;

#if !NET40
using System.Runtime.CompilerServices;
#else
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace CodeOnlyStoredProcedure.Dynamic
{
    internal class DynamicStoredProcedureResultsAwaiter : DynamicObject
#if !NET40
        , ICriticalNotifyCompletion
#endif
    {
        private readonly DynamicStoredProcedureResults results;
        private readonly Task                          toWait;

#if NET40
        /// <summary>
        /// The type to use as an awaiter. Because the ICriticalNotifyCompletion interface is 
        /// available to .NET 4.0 with the Microsoft Async package, we can support awaiting a 
        /// dynamic stored procedure if we implement that interface. I don't want to require 
        /// async for CodeOnlyStoerdProcedure in .NET 4.0, so this is a decent compromise. All  
        /// the functionality is in the implementation in the Async Targeting Pack, so we just
        /// have to generate a dynamic assembly that can be used to call it.
        /// </summary>
        private static Lazy<Type> dynamicAwaiterType = new Lazy<Type>(() =>
        {
            var baseType  = typeof(DynamicStoredProcedureResultsAwaiter);
            var ifaceType = Type.GetType("System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Threading.Tasks");

            if (ifaceType == null)
                throw new NotSupportedException("Could not find the interface required for using await in .NET 4. Please make sure that System.Threading.Tasks is loaded in your process before trying to await a dynamic stored procedure.");

            var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("CodeOnlyStoredProcedures.Net40Async"),
                AssemblyBuilderAccess.Run);

            var mod  = ab.DefineDynamicModule("CodeOnlyStoredProcedures.Net40Async");
            var type = mod.DefineType("DynamicStoredProcedureResultsAwaiterImpl", 
                                      TypeAttributes.Class | TypeAttributes.NotPublic,
                                      baseType);
            
            var extensionsType          = Type.GetType("AwaitExtensions, Microsoft.Threading.Tasks");
            var configuredAwaitableType = Type.GetType("Microsoft.Runtime.CompilerServices.ConfiguredTaskAwaitable, Microsoft.Threading.Tasks");
            var configuredAwaiterType   = configuredAwaitableType.GetNestedType("ConfiguredTaskAwaiter");

            var awaiterField = type.DefineField("awaiter", configuredAwaiterType, FieldAttributes.Private | FieldAttributes.InitOnly);

            var ctor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] { typeof(DynamicStoredProcedureResults), typeof(Task), typeof(bool) });
            var il   = ctor.GetILGenerator();
            var temp = il.DeclareLocal(configuredAwaitableType);
            
            il.Emit(OpCodes.Ldarg_0); // push "this"
            il.Emit(OpCodes.Ldarg_1); // push the rest of the parameters
            il.Emit(OpCodes.Ldarg_2);

            // call the base ctor
            il.Emit(OpCodes.Call, baseType.GetConstructor(new[] { typeof(DynamicStoredProcedureResults), typeof(Task) }));

            // store this on the stack, so after the configure and get await calls, it will be ready to set the field
            il.Emit(OpCodes.Ldarg_0);

            // call ConfigureAwait on the task
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Call, extensionsType.GetMethod("ConfigureAwait", new[] { typeof(Task), typeof(bool) }));
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloca_S, temp);
            // call GetAwaiter on the ConfiguredTaskAwaitable
            il.Emit(OpCodes.Call, configuredAwaitableType.GetMethod("GetAwaiter"));
            // store the value in the field
            il.Emit(OpCodes.Stfld, awaiterField);
            il.Emit(OpCodes.Ret);

            // define the 2 methods implemented in ICriticalNotifyCompletion

            // void OnCompleted(Action continuation)
            var method = type.DefineMethod("OnCompleted", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.HasThis, typeof(void), new[] { typeof(Action) });
            il = method.GetILGenerator();
            temp = il.DeclareLocal(configuredAwaiterType);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, awaiterField);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloca_S, temp);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, configuredAwaiterType.GetMethod("OnCompleted"));
            il.Emit(OpCodes.Ret);

            // void UnsafeOnCompleted(Action continuation)
            method = type.DefineMethod("UnsafeOnCompleted", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.HasThis, typeof(void), new[] { typeof(Action) });
            il = method.GetILGenerator();
            temp = il.DeclareLocal(configuredAwaiterType);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, awaiterField);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloca_S, temp);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, configuredAwaiterType.GetMethod("UnsafeOnCompleted"));
            il.Emit(OpCodes.Ret);            

            // we add this interface here instead of in the DefineType method, because this will automatically 
            // associate the base type's OnCompleted method as the implementing method; DefineType doesn't do so.
            type.AddInterfaceImplementation(ifaceType);

            return type.CreateType();
        });
#endif

        public static object Create(DynamicStoredProcedureResults results, Task task, bool continueOnCaller)
        {
            Contract.Requires(results != null);
            Contract.Requires(task    != null);
            Contract.Ensures (Contract.Result<object>() != null);
#if NET40
            return Activator.CreateInstance(dynamicAwaiterType.Value, results, task, continueOnCaller);
#else
            return new DynamicStoredProcedureResultsAwaiter(results, task, continueOnCaller);
#endif
        }

#if !NET40
        private readonly ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter;
        public DynamicStoredProcedureResultsAwaiter(
            DynamicStoredProcedureResults results, 
            Task                          toWait,
            bool                          continueOnCaller)
            : this(results, toWait)
        {
            Contract.Requires(results != null);
            Contract.Requires(toWait  != null);

            this.awaiter = toWait.ConfigureAwait(continueOnCaller).GetAwaiter();
        }
#endif

        public DynamicStoredProcedureResultsAwaiter(
            DynamicStoredProcedureResults results, 
            Task                          toWait)
        {
            Contract.Requires(results != null);
            Contract.Requires(toWait  != null);

            this.results = results;
            this.toWait  = toWait;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (binder.Name == "IsCompleted")
            {
                if (toWait.Status == TaskStatus.Faulted)
                    throw toWait.Exception;

                result = toWait.IsCompleted;
                return true;
            }

            return base.TryGetMember(binder, out result);
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (binder.Name == "GetResult")
            {
                if (toWait.Status == TaskStatus.Faulted)
                    throw toWait.Exception;

                result = results;
                return true;
            }

            return base.TryInvokeMember(binder, args, out result);
        }

        // These methods are not defined for .NET 4.0, since the awaiter isn't going to be available, either.
        // Therefore, they have to be implemented on the dynamically constructed type.
#if !NET40
        public void OnCompleted(Action continuation) => awaiter.OnCompleted(continuation);
        public void UnsafeOnCompleted(Action continuation) => awaiter.UnsafeOnCompleted(continuation);
#endif
    }
}
