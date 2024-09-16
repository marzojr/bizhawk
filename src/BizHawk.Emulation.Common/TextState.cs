#nullable disable

using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Emulation.Common
{
	// managed counterpart to unmanaged serialization code in GB and WSWAN cores

	// T is a class that has any other data you want to serialize in it
	public class TextState<T>
		where T : new()
	{
		public TextState()
		{
			ExtraData = new T();
		}

		public class Node
		{
			public readonly Dictionary<string, byte[]> Data = new Dictionary<string, byte[]>();
			public readonly Dictionary<string, Node> Objects = new Dictionary<string, Node>();

			// methods named "ShouldSerialize*" are detected and dynamically invoked by JSON.NET
			// if they return false during serialization, the field/prop is omitted from the created json
			// ReSharper disable once UnusedMember.Global
			public bool ShouldSerializeData()
			{
				return Data.Count > 0;
			}

			// ReSharper disable once UnusedMember.Global
			public bool ShouldSerializeObjects()
			{
				return Objects.Count > 0;
			}
		}

		public readonly Node Root = new Node();

		private Stack<Node> Nodes;

		private Node Current => Nodes.Peek();

		public void Prepare()
		{
			Nodes = new Stack<Node>();
			Nodes.Push(Root);
		}

		public void Save(IntPtr data, int length, string name)
		{
			byte[] d = new byte[length];
			Marshal.Copy(data, d, 0, length);
			Current.Data.Add(name, d); // will except for us if the key is already present
		}

		/// <exception cref="InvalidOperationException"><paramref name="length"/> does not match the length of the data saved as <paramref name="name"/></exception>
		public void Load(IntPtr data, int length, string name)
		{
			byte[] d = Current.Data[name];
			if (length != d.Length)
			{
				throw new InvalidOperationException();
			}

			Marshal.Copy(d, 0, data, length);
		}

		public void EnterSectionSave(string name)
		{
			Node next = new Node();
			Current.Objects.Add(name, next);
			Nodes.Push(next);
		}

		public void EnterSectionLoad(string name)
		{
			Node next = Current.Objects[name];
			Nodes.Push(next);
		}

		public void EnterSection(string name)
		{
			// works for either save or load, but as a consequence cannot report intelligent
			// errors about section name mismatches
			Nodes.Push(Current.Objects.GetValueOrPutNew(name));
		}

		/// <exception cref="InvalidOperationException"><paramref name="name"/> doesn't match the section being closed</exception>
		public void ExitSection(string name)
		{
			Node last = Nodes.Pop();
			if (Current.Objects[name] != last)
			{
				throw new InvalidOperationException();
			}
		}

		// other data besides the core
		public readonly T ExtraData;

		public TextStateFPtrs GetFunctionPointersSave()
		{
			return new TextStateFPtrs
			{
				Save = Save,
				Load = null,
				EnterSection = EnterSectionSave,
				ExitSection = ExitSection
			};
		}

		public TextStateFPtrs GetFunctionPointersLoad()
		{
			return new TextStateFPtrs
			{
				Save = null,
				Load = Load,
				EnterSection = EnterSectionLoad,
				ExitSection = ExitSection
			};
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct TextStateFPtrs
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void DataFunction(IntPtr data, int length, string name);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SectionFunction(string name);

		public DataFunction Save;
		public DataFunction Load;
		public SectionFunction EnterSection;
		public SectionFunction ExitSection;
	}
}
