using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable NotAccessedField.Local

#pragma warning disable CS0414
#pragma warning disable CS0649

namespace BizHawk.BizInvoke
{
	public static unsafe class BizInvokerUtilities
	{
		[StructLayout(LayoutKind.Explicit)]
		private class U
		{
			[FieldOffset(0)]
			public readonly U1? First;

			[FieldOffset(0)]
			public readonly U2 Second;

			public U(U2 second)
			{
				// being a union type, these assignments affect the value of both fields so they need to be in this order
				First = null;
				Second = second;
			}
		}

		[StructLayout(LayoutKind.Explicit)]
		private class U1
		{
			[FieldOffset(0)]
			public UIntPtr P;
		}

		[StructLayout(LayoutKind.Explicit)]
		private class U2
		{
			[FieldOffset(0)]
			public object Target;

			public U2(object target) => Target = target;
		}

		private class CF
		{
			public int FirstField = 0;
		}

		/// <summary>
		/// Computes the byte offset of the first field of any class relative to a class pointer.
		/// </summary>
		public static int ComputeClassFirstFieldOffset()
			=> ComputeFieldOffset(typeof(CF).GetField("FirstField")!);

		/// <summary>
		/// Compute the byte offset of the first byte of string data (UTF16) relative to a pointer to the string.
		/// </summary>
		public static int ComputeStringOffset()
		{
			var s = new string(Array.Empty<char>());
			int ret;
			fixed(char* fx = s)
			{
				U u = new(new(s));
				ret = (int) ((ulong) (UIntPtr) fx - (ulong) u.First!.P);
			}
			return ret;
		}

		/// <summary>
		/// Compute the offset to the 0th element of an array of value types
		/// </summary>
		public static int ComputeValueArrayElementOffset()
		{
			var arr = new int[4];
			int ret;
			fixed (int* p = arr)
			{
				U u = new(new(arr));
				ret = (int)((ulong)(UIntPtr) p - (ulong) u.First!.P);
			}
			return ret;
		}

#if false
		/// <summary>
		/// Compute the offset to the 0th element of an array of object types
		/// Slow, so cache it if you need it.
		/// </summary>
		public static int ComputeObjectArrayElementOffset()
		{
			var obj = new object[4];
			var method = new DynamicMethod("ComputeObjectArrayElementOffsetHelper", typeof(int), new[] { typeof(object[]) }, typeof(string).Module, true);
			var il = method.GetILGenerator();
			var local = il.DeclareLocal(typeof(object[]), true);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Stloc, local);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Ldelema, typeof(object));
			il.Emit(OpCodes.Conv_I);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Conv_I);
			il.Emit(OpCodes.Sub);
			il.Emit(OpCodes.Conv_I4);
			il.Emit(OpCodes.Ret);
			var del = (Func<object[], int>)method.CreateDelegate(typeof(Func<object[], int>));
			return del(obj);
		}
#endif

		/// <summary>
		/// Compute the byte offset of a field relative to a pointer to the class instance.
		/// Slow, so cache it if you need it.
		/// </summary>
		public static int ComputeFieldOffset(FieldInfo fi)
		{
			if (fi.DeclaringType!.IsValueType)
			{
				throw new NotImplementedException("Only supported for class fields right now");
			}

			var obj = FormatterServices.GetUninitializedObject(fi.DeclaringType);
			var method = new DynamicMethod("ComputeFieldOffsetHelper", typeof(int), new[] { typeof(object) }, typeof(string).Module, true);
			var il = method.GetILGenerator();
			var local = il.DeclareLocal(fi.DeclaringType, true);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Stloc, local);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldflda, fi);
			il.Emit(OpCodes.Conv_I);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Conv_I);
			il.Emit(OpCodes.Sub);
			il.Emit(OpCodes.Conv_I4);
			il.Emit(OpCodes.Ret);
			var del = (Func<object, int>)method.CreateDelegate(typeof(Func<object, int>));
			return del(obj);
		}
	}
}
