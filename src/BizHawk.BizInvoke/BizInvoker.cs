using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.StringExtensions;

namespace BizHawk.BizInvoke
{
	public static class BizInvoker
	{
		private static readonly MethodInfo MInfo_Encoding_GetByteCount = typeof(Encoding).GetMethod(nameof(Encoding.GetByteCount), new[] { typeof(string) })!;

		private static readonly MethodInfo MInfo_Encoding_GetBytes = typeof(Encoding).GetMethod(nameof(Encoding.GetBytes), new[] { typeof(char*), typeof(int), typeof(byte*), typeof(int) })!;

		private static readonly MethodInfo MInfo_ICallingConventionAdapter_GetFunctionPointerForDelegate = typeof(ICallingConventionAdapter).GetMethod(nameof(ICallingConventionAdapter.GetFunctionPointerForDelegate))!;

		private static readonly MethodInfo MInfo_IMonitor_Enter = typeof(IMonitor).GetMethod(nameof(IMonitor.Enter))!;

		private static readonly MethodInfo MInfo_IMonitor_Exit = typeof(IMonitor).GetMethod(nameof(IMonitor.Exit))!;

		/// <summary>
		/// holds information about a proxy implementation, including type and setup hooks
		/// </summary>
		private class InvokerImpl
		{
			private readonly Action<object, ICallingConventionAdapter> _connectCallingConventionAdapter;

			private readonly Action<object, IMonitor>? _connectMonitor;

			private readonly List<Action<object, IImportResolver, ICallingConventionAdapter>> _hooks;

			private readonly Type _implType;

			public readonly bool IsMonitored;

			public InvokerImpl(
				List<Action<object, IImportResolver, ICallingConventionAdapter>> hooks,
				Type implType,
				Action<object, IMonitor>? connectMonitor,
				Action<object, ICallingConventionAdapter> connectCallingConventionAdapter)
			{
				_connectCallingConventionAdapter = connectCallingConventionAdapter;
				_connectMonitor = connectMonitor;
				_hooks = hooks;
				_implType = implType;
				IsMonitored = connectMonitor != null;
			}

			public object Create(IImportResolver dll, IMonitor? monitor, ICallingConventionAdapter adapter)
			{
				var ret = Activator.CreateInstance(_implType)!;
				_connectCallingConventionAdapter(ret, adapter);
				foreach (var f in _hooks)
				{
					f(ret, dll, adapter);
				}
				_connectMonitor?.Invoke(ret, monitor!);
				return ret;
			}
		}

		/// <summary>
		/// dictionary of all generated proxy implementations and their base types
		/// </summary>
		private static readonly IDictionary<Type, InvokerImpl> Impls = new Dictionary<Type, InvokerImpl>();

		/// <summary>
		/// the assembly that all proxies are placed in
		/// </summary>
		private static readonly AssemblyBuilder ImplAssemblyBuilder;

		/// <summary>
		/// the module that all proxies are placed in
		/// </summary>
		private static readonly ModuleBuilder ImplModuleBuilder;

		/// <summary>
		/// How far into a class pointer the first field is.  Different on mono and fw.
		/// </summary>
		private static readonly int ClassFieldOffset;

		/// <summary>
		/// How far into a string pointer the first chair is.
		/// </summary>
		private static readonly int StringOffset;

		/// <summary>
		/// How far into a value array type element 0 is.
		/// </summary>
		private static readonly int ValueArrayElementOffset;

		static BizInvoker()
		{
			var aname = new AssemblyName("BizInvokeProxyAssembly");
			ImplAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(aname, AssemblyBuilderAccess.Run);
			ImplModuleBuilder = ImplAssemblyBuilder.DefineDynamicModule("BizInvokerModule");
			ClassFieldOffset = BizInvokerUtilities.ComputeClassFirstFieldOffset();
			StringOffset = BizInvokerUtilities.ComputeStringOffset();
			ValueArrayElementOffset = BizInvokerUtilities.ComputeValueArrayElementOffset();
		}

		/// <summary>
		/// get an implementation proxy for an interop class
		/// </summary>
		/// <typeparam name="T">The class type that represents the DLL</typeparam>
		/// <exception cref="InvalidOperationException"><see cref="GetInvoker{T}(BizHawk.Common.IImportResolver,BizHawk.Common.IMonitor,BizHawk.BizInvoke.ICallingConventionAdapter)"/> overload previously called with <paramref name="dll"/></exception>
		public static T GetInvoker<T>(IImportResolver dll, ICallingConventionAdapter adapter)
			where T : class
		{
			var nonTrivialAdapter = adapter.GetType() != CallingConventionAdapters.Native.GetType();
			InvokerImpl impl;
			lock (Impls) impl = Impls.GetValueOrPut(
				typeof(T),
				baseType => CreateProxy(baseType, monitor: false, nonTrivialAdapter: nonTrivialAdapter));
			if (impl.IsMonitored)
			{
				throw new InvalidOperationException("Class was previously proxied with a monitor!");
			}

			return (T)impl.Create(dll, null, adapter);
		}

		/// <exception cref="InvalidOperationException">this method was previously called with <paramref name="dll"/></exception>
		public static T GetInvoker<T>(IImportResolver dll, IMonitor monitor, ICallingConventionAdapter adapter)
			where T : class
		{
			var nonTrivialAdapter = adapter.GetType() != CallingConventionAdapters.Native.GetType();
			InvokerImpl impl;
			lock (Impls) impl = Impls.GetValueOrPut(
				typeof(T),
				baseType => CreateProxy(baseType, monitor: true, nonTrivialAdapter: nonTrivialAdapter));
			if (!impl.IsMonitored)
			{
				throw new InvalidOperationException("Class was previously proxied without a monitor!");
			}

			return (T)impl.Create(dll, monitor, adapter);
		}

		private static InvokerImpl CreateProxy(Type baseType, bool monitor, bool nonTrivialAdapter)
		{
			if (baseType.IsSealed)
			{
				throw new InvalidOperationException("Can't proxy a sealed type");
			}

			if (!baseType.IsPublic)
			{
				// the proxy type will be in a new assembly, so public is required here
				throw new InvalidOperationException("Type must be public");
			}

			_ = baseType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)
				?? throw new InvalidOperationException("Base type must have a zero arg constructor");

			var baseMethods = baseType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
				.Select(static m => (Info: m, Attr: m.GetCustomAttributes(true).OfType<BizImportAttribute>().FirstOrDefault()))
				.Where(a => a.Attr != null)
				.ToList();

			if (baseMethods.Count == 0)
			{
				throw new InvalidOperationException("Couldn't find any [BizImport] methods to proxy");
			}

			{
				var uo = baseMethods.FirstOrNull(static a => !a.Info.IsVirtual || a.Info.IsFinal);
				if (uo is not null) throw new InvalidOperationException($"Method {uo.Value.Info.Name} cannot be overriden!");

				// there's no technical reason to disallow this, but we wouldn't be doing anything
				// with the base implementation, so it's probably a user error
				var na = baseMethods.FirstOrNull(static a => !a.Info.IsAbstract);
				if (na is not null) throw new InvalidOperationException($"Method {na.Value.Info.Name} is not abstract!");
			}

			// hooks that will be run on the created proxy object
			var postCreateHooks = new List<Action<object, IImportResolver, ICallingConventionAdapter>>();

			var type = ImplModuleBuilder.DefineType($"Bizhawk.BizInvokeProxy{baseType.Name}", TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed, baseType);

			var monitorField = monitor ? type.DefineField("MonitorField", typeof(IMonitor), FieldAttributes.Public) : null;

			var adapterField = type.DefineField("CallingConvention", typeof(ICallingConventionAdapter), FieldAttributes.Public);

			foreach (var (info, attr) in baseMethods)
			{
				var entryPointName = attr!.EntryPoint ?? info.Name;

				var hook = attr.Compatibility
					? ImplementMethodDelegate(type, info, attr.CallingConvention, entryPointName, monitorField, nonTrivialAdapter)
					: ImplementMethodCalli(type, info, attr.CallingConvention, entryPointName, monitorField, adapterField);

				postCreateHooks.Add(hook);
			}

			return new(
				postCreateHooks,
				type.CreateType()!,
				connectMonitor: monitor
					? (o, m) => o.GetType().GetField(monitorField!.Name).SetValue(o, m)
					: null,
				connectCallingConventionAdapter: (o, a) => o.GetType().GetField(adapterField.Name).SetValue(o, a));
		}

		/// <summary>
		/// create a method implementation that uses GetDelegateForFunctionPointer internally
		/// </summary>
		private static Action<object, IImportResolver, ICallingConventionAdapter> ImplementMethodDelegate(
			TypeBuilder type,
			MethodInfo baseMethod,
			CallingConvention nativeCall,
			string entryPointName,
			FieldInfo? monitorField,
			bool nonTrivialAdapter)
		{
			// create the delegate type
			var delegateType = BizInvokeUtilities.CreateDelegateType(baseMethod, nativeCall, type, out var delegateInvoke);

			var paramInfos = baseMethod.GetParameters();
			var paramTypes = paramInfos.Select(p => p.ParameterType).ToArray();
			var returnType = baseMethod.ReturnType;

			static Type[]? NullifyIfEmpty(Type[]? t)
				=> t?.Length == 0 ? null : t;

			static Type[]?[]? NullifyIfAllNull(Type[]?[] t)
				=> t.All(t => t == null) ? null : t;

			// this stuff is required for in
			var paramTypeRequiredCustomModifiers = NullifyIfAllNull(paramInfos.Select(pi => NullifyIfEmpty(pi.GetRequiredCustomModifiers())).ToArray());
			var paramTypeOptionalCustomModifiers = NullifyIfAllNull(paramInfos.Select(pi => NullifyIfEmpty(pi.GetOptionalCustomModifiers())).ToArray());
			var returnTypeRequiredCustomModifiers = NullifyIfEmpty(baseMethod.ReturnParameter?.GetRequiredCustomModifiers());
			var returnTypeOptionalCustomModifiers = NullifyIfEmpty(baseMethod.ReturnParameter?.GetRequiredCustomModifiers());

			if (paramTypes.Concat(new[] { returnType }).Any(typeof(Delegate).IsAssignableFrom))
			{
				// this isn't a problem if CallingConventionAdapters.Waterbox is a no-op, but it is otherwise:  we don't
				// have a custom marshaller set up so the user needs to manually pump the callingconventionadapter
				if (nonTrivialAdapter)
				{
					throw new InvalidOperationException(
						"Compatibility call mode cannot use ICallingConventionAdapters for automatically marshalled delegate types!");
				}
			}

			// define a field on the class to hold the delegate
			var field = type.DefineField(
				$"DelegateField{baseMethod.Name}",
				delegateType,
				FieldAttributes.Public);

			var method = type.DefineMethod(
				baseMethod.Name,
				MethodAttributes.Virtual | MethodAttributes.Public,
				CallingConventions.HasThis,
				returnType,
				returnTypeRequiredCustomModifiers,
				returnTypeOptionalCustomModifiers,
				paramTypes,
				paramTypeRequiredCustomModifiers,
				paramTypeOptionalCustomModifiers);

			var il = method.GetILGenerator();

			Label exc = default;
			if (monitorField != null) // monitor: enter and then begin try
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, monitorField);
				il.Emit(OpCodes.Callvirt, MInfo_IMonitor_Enter);
				exc = il.BeginExceptionBlock();
			}

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, field);
			for (var i = 0; i < paramTypes.Length; i++)
			{
				il.Emit(OpCodes.Ldarg, (short)(i + 1));
			}

			il.Emit(OpCodes.Callvirt, delegateInvoke);

			if (monitorField != null) // monitor: finally exit
			{
				LocalBuilder? loc = null;
				if (returnType != typeof(void))
				{
					loc = il.DeclareLocal(returnType);
					il.Emit(OpCodes.Stloc, loc);
				}

				il.Emit(OpCodes.Leave, exc);
				il.BeginFinallyBlock();
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, monitorField);
				il.Emit(OpCodes.Callvirt, MInfo_IMonitor_Exit);
				il.EndExceptionBlock();

				if (returnType != typeof(void))
				{
					il.Emit(OpCodes.Ldloc, loc!);
				}
			}

			il.Emit(OpCodes.Ret);

			type.DefineMethodOverride(method, baseMethod);

			return (o, dll, adapter) =>
			{
				var entryPtr = dll.GetProcAddrOrThrow(entryPointName);
				var interopDelegate = adapter.GetDelegateForFunctionPointer(entryPtr, delegateType.CreateType());
				o.GetType().GetField(field.Name).SetValue(o, interopDelegate);
			};
		}

		private readonly struct ParameterLoadInfo
		{
			/// <summary>
			/// The native type for this parameter, to pass to calli
			/// </summary>
			public readonly Type NativeType;

			/// <summary>
			/// Closure that will actually emit the parameter load to the il stream.  The evaluation stack will
			/// already have other parameters on it at this time.
			/// </summary>
			public readonly Action EmitLoad;

			public ParameterLoadInfo(Type nativeType, Action emitLoad)
			{
				NativeType = nativeType;
				EmitLoad = emitLoad;
			}
		}

		/// <summary>
		/// create a method implementation that uses calli internally
		/// </summary>
		private static Action<object, IImportResolver, ICallingConventionAdapter> ImplementMethodCalli(
			TypeBuilder type,
			MethodInfo baseMethod,
			CallingConvention nativeCall,
			string entryPointName,
			FieldInfo? monitorField,
			FieldInfo adapterField)
		{
			var paramInfos = baseMethod.GetParameters();
			var paramTypes = paramInfos.Select(p => p.ParameterType).ToArray();
			var returnType = baseMethod.ReturnType;
			if (returnType != typeof(void) && returnType is { IsPrimitive: false, IsPointer: false, IsEnum: false })
			{
				throw new InvalidOperationException("Only primitive return types are supported");
			}

			// define a field on the type to hold the entry pointer
			var field = type.DefineField(
				$"EntryPtrField{baseMethod.Name}",
				typeof(IntPtr),
				FieldAttributes.Public);

			var method = type.DefineMethod(
				baseMethod.Name,
				MethodAttributes.Virtual | MethodAttributes.Public,
				CallingConventions.HasThis,
				returnType,
				paramTypes);

			var il = method.GetILGenerator();

			Label exc = default;
			if (monitorField != null) // monitor: enter and then begin try
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, monitorField);
				il.Emit(OpCodes.Callvirt, MInfo_IMonitor_Enter);
				exc = il.BeginExceptionBlock();
			}

			// phase 1:  empty eval stack and each parameter load thunk does any prep work it needs to do
			var paramLoadInfos = paramTypes
				.Select(
					// arg 0 is this, so + 1
					(t, i) => EmitParamterLoad(il, i + 1, t, adapterField))
				.ToArray();

			// phase 2:  actually load the individual params, leaving each one on the stack
			foreach (var pli in paramLoadInfos)
			{
				pli.EmitLoad();
			}

			bool WantsWinAPIBool()
			{
				var attrs = baseMethod.ReturnTypeCustomAttributes.GetCustomAttributes(typeof(MarshalAsAttribute), false);
				return attrs.Length > 0 && ((MarshalAsAttribute)attrs[0]).Value is UnmanagedType.Bool;
			}

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, field);
			il.EmitCalli(
				OpCodes.Calli,
				nativeCall,
				returnType != typeof(bool) || WantsWinAPIBool()
				? returnType
				: typeof(byte), // undo winapi style bool garbage by default
				paramLoadInfos.Select(p => p.NativeType).ToArray());

			if (monitorField != null) // monitor: finally exit
			{
				LocalBuilder? loc = null;
				if (returnType != typeof(void))
				{
					loc = il.DeclareLocal(returnType);
					il.Emit(OpCodes.Stloc, loc);
				}

				il.Emit(OpCodes.Leave, exc);
				il.BeginFinallyBlock();
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, monitorField);
				il.Emit(OpCodes.Callvirt, MInfo_IMonitor_Exit);
				il.EndExceptionBlock();

				if (returnType != typeof(void))
				{
					il.Emit(OpCodes.Ldloc, loc!);
				}
			}

			// either there's a primitive on the stack and we're expected to return that primitive,
			// or there's nothing on the stack and we're expected to return nothing
			il.Emit(OpCodes.Ret);

			type.DefineMethodOverride(method, baseMethod);

			return (o, dll, adapter) =>
			{
				var entryPtr = dll.GetProcAddrOrThrow(entryPointName);
				o.GetType().GetField(field.Name).SetValue(
					o, adapter.GetDepartureFunctionPointer(entryPtr, new(returnType, paramTypes), o));
			};
		}

		/// <summary>
		/// load an IntPtr constant in an IL stream
		/// </summary>
		private static void LoadConstant(ILGenerator il, IntPtr p)
		{
			if (p == IntPtr.Zero)
			{
				il.Emit(OpCodes.Ldc_I4_0);
			}
			else if (IntPtr.Size == 4)
			{
				il.Emit(OpCodes.Ldc_I4, (int)p);
			}
			else
			{
				il.Emit(OpCodes.Ldc_I8, (long)p);
			}

			il.Emit(OpCodes.Conv_I);
		}

#if false
		/// <summary>
		/// load a UIntPtr constant in an IL stream
		/// </summary>
		private static void LoadConstant(ILGenerator il, UIntPtr p)
		{
			if (p == UIntPtr.Zero)
			{
				il.Emit(OpCodes.Ldc_I4_0);
			}
			else if (UIntPtr.Size == 4)
			{
				il.Emit(OpCodes.Ldc_I4, (int)p);
			}
			else
			{
				il.Emit(OpCodes.Ldc_I8, (long)p);
			}

			il.Emit(OpCodes.Conv_U);
		}
#endif

		/// <summary>
		/// emit a single parameter load with unmanaged conversions.  The evaluation stack will be empty when the IL generated here runs,
		/// and should end as empty.
		/// </summary>
		private static ParameterLoadInfo EmitParamterLoad(ILGenerator il, int idx, Type type, FieldInfo adapterField)
		{
			if (type.IsGenericType)
			{
				throw new NotImplementedException("Generic types not supported");
			}

			if (type.IsByRef)
			{
				// Just pass a raw pointer.  In the `ref structType` case, caller needs to ensure fields are compatible.
				var et = type.GetElementType()!;
				if (!et.IsValueType)
				{
					throw new NotImplementedException("Only refs of value types are supported!");
				}
				return new(
					typeof(IntPtr),
					() =>
					{
						var loc = il.DeclareLocal(type, true);
						il.Emit(OpCodes.Ldarg, (short)idx);
						il.Emit(OpCodes.Dup);
						il.Emit(OpCodes.Stloc, loc);
						il.Emit(OpCodes.Conv_I);
					});
			}

			if (type.IsArray)
			{
				var et = type.GetElementType()!;
				if (!et.IsValueType)
				{
					throw new NotImplementedException("Only arrays of value types are supported!");
				}

				// these two cases aren't too hard to add
				if (type.GetArrayRank() > 1)
				{
					throw new NotImplementedException("Multidimensional arrays are not supported!");
				}

				if (type.Name.ContainsOrdinal('*'))
				{
					throw new NotImplementedException("Only 0-based 1-dimensional arrays are supported!");
				}

				return new(
					typeof(IntPtr),
					() =>
					{
						var loc = il.DeclareLocal(type, true);
						var end = il.DefineLabel();
						var isNull = il.DefineLabel();

						il.Emit(OpCodes.Ldarg, (short)idx);
						il.Emit(OpCodes.Brfalse, isNull);

						il.Emit(OpCodes.Ldarg, (short)idx);
						il.Emit(OpCodes.Dup);
						il.Emit(OpCodes.Stloc, loc);
						il.Emit(OpCodes.Conv_I);
						il.Emit(OpCodes.Ldc_I4, ValueArrayElementOffset);
						il.Emit(OpCodes.Conv_I);
						il.Emit(OpCodes.Add);
						il.Emit(OpCodes.Br, end);

						il.MarkLabel(isNull);
						LoadConstant(il, IntPtr.Zero);
						il.MarkLabel(end);
					});
			}

			if (typeof(Delegate).IsAssignableFrom(type))
			{
				// callback -- use the same callingconventionadapter on it that the invoker is being made from
				return new(
					typeof(IntPtr),
					() =>
					{
						var mi = MInfo_ICallingConventionAdapter_GetFunctionPointerForDelegate;
						var end = il.DefineLabel();
						var isNull = il.DefineLabel();

						il.Emit(OpCodes.Ldarg, (short)idx);
						il.Emit(OpCodes.Brfalse, isNull);

						il.Emit(OpCodes.Ldarg_0);
						il.Emit(OpCodes.Ldfld, adapterField);
						il.Emit(OpCodes.Ldarg, (short)idx);
						il.EmitCall(OpCodes.Callvirt, mi, Type.EmptyTypes);
						il.Emit(OpCodes.Br, end);

						il.MarkLabel(isNull);
						LoadConstant(il, IntPtr.Zero);
						il.MarkLabel(end);
					});
			}

			if (type == typeof(string))
			{
				var end = il.DefineLabel();
				var isNull = il.DefineLabel();

				il.Emit(OpCodes.Ldarg, (short)idx);
				il.Emit(OpCodes.Brfalse, isNull);

				var encoding = il.DeclareLocal(typeof(Encoding), false);
				il.EmitCall(OpCodes.Call, typeof(Encoding).GetProperty("UTF8")!.GetGetMethod()!, Type.EmptyTypes);
				il.Emit(OpCodes.Stloc, encoding);

				var strlenbytes = il.DeclareLocal(typeof(int), false);
				il.Emit(OpCodes.Ldloc, encoding);
				il.Emit(OpCodes.Ldarg, (short)idx);
				il.EmitCall(OpCodes.Callvirt, MInfo_Encoding_GetByteCount, Type.EmptyTypes);
				il.Emit(OpCodes.Stloc, strlenbytes);

				var strval = il.DeclareLocal(typeof(string), true); // pin!
				il.Emit(OpCodes.Ldarg, (short)idx);
				il.Emit(OpCodes.Stloc, strval);

				var bytes = il.DeclareLocal(typeof(IntPtr));
				il.Emit(OpCodes.Ldloc, strlenbytes);
				il.Emit(OpCodes.Ldc_I4_1);
				il.Emit(OpCodes.Add); // +1 for null byte
				il.Emit(OpCodes.Conv_U);
				// NB: The evaluation stack must be entirely empty, except for the size argument, when calling localloc.
				// That's why we have to split every parameter load into two parts, the first of which runs on an empty stack.
				il.Emit(OpCodes.Localloc);
				il.Emit(OpCodes.Stloc, bytes);

				// this
				il.Emit(OpCodes.Ldloc, encoding);
				// chars
				il.Emit(OpCodes.Ldloc, strval);
				il.Emit(OpCodes.Conv_U);
				il.Emit(OpCodes.Ldc_I4, StringOffset);
				il.Emit(OpCodes.Add);
				// charcount
				il.Emit(OpCodes.Ldloc, strval);
				il.Emit(OpCodes.Call, typeof(string).GetProperty("Length")!.GetGetMethod()!);
				// bytes
				il.Emit(OpCodes.Ldloc, bytes);
				// bytelength
				il.Emit(OpCodes.Ldloc, strlenbytes);
				// call
				il.EmitCall(OpCodes.Callvirt, MInfo_Encoding_GetBytes, Type.EmptyTypes);
				// unused ret
				il.Emit(OpCodes.Pop);

				il.Emit(OpCodes.Br, end);

				il.MarkLabel(isNull);
				LoadConstant(il, IntPtr.Zero);
				il.Emit(OpCodes.Stloc, bytes);
				il.MarkLabel(end);

				return new(
					typeof(IntPtr),
					() =>
					{
						il.Emit(OpCodes.Ldloc, bytes);
					});
			}

			if (type.IsClass)
			{
				// non ref of class can just be passed as pointer
				// Just like in the `ref struct` case, if the fields aren't compatible, that's the caller's problem.
				return new(
					typeof(IntPtr),
					() =>
					{
						var loc = il.DeclareLocal(type, true);
						var end = il.DefineLabel();
						var isNull = il.DefineLabel();

						il.Emit(OpCodes.Ldarg, (short)idx);
						il.Emit(OpCodes.Brfalse, isNull);

						il.Emit(OpCodes.Ldarg, (short)idx);
						il.Emit(OpCodes.Dup);
						il.Emit(OpCodes.Stloc, loc);
						il.Emit(OpCodes.Conv_I);
						// skip past the methodtable pointer to the first field
						il.Emit(OpCodes.Ldc_I4, ClassFieldOffset);
						il.Emit(OpCodes.Conv_I);
						il.Emit(OpCodes.Add);
						il.Emit(OpCodes.Br, end);

						il.MarkLabel(isNull);
						LoadConstant(il, IntPtr.Zero);
						il.MarkLabel(end);
					});
			}

			if (type.IsPrimitive || type.IsEnum || type.IsPointer)
			{
				return new(
					type,
					() =>
					{
						il.Emit(OpCodes.Ldarg, (short)idx);
					});
			}

			throw new NotImplementedException("Unrecognized parameter type!");
		}
	}

	/// <summary>Indicates that an abstract method is to be proxied by BizInvoker.</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class BizImportAttribute : Attribute
	{
		public CallingConvention CallingConvention { get; }

		/// <remarks>The annotated method's name is used iff <see langword="null"/>.</remarks>
		public string? EntryPoint { get; set; }

		/// <summary><see langword="true"/> iff a compatibility interop should be used, which is slower but supports more argument types.</summary>
		public bool Compatibility { get; set; }

		public BizImportAttribute(CallingConvention c)
		{
			CallingConvention = c;
		}
	}
}
