using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	/// <summary>Implementors are able to provide pointers to functions in dynamically-linked libraries, which are loaded through some undefined mechanism.</summary>
	public interface IImportResolver
	{
		IntPtr GetProcAddrOrZero(string entryPoint);

		/// <exception cref="InvalidOperationException">could not find symbol</exception>
		IntPtr GetProcAddrOrThrow(string entryPoint);
	}

	public class DynamicLibraryImportResolver : IDisposable, IImportResolver
	{
		private IntPtr _p;

		public readonly bool HasLimitedLifetime;

		/// <param name="hasLimitedLifetime">will never be unloaded iff false (like <see cref="DllImportAttribute">[DllImport]</see>)</param>
		public DynamicLibraryImportResolver(string dllName, bool hasLimitedLifetime = true)
		{
			// on Windows, SetDllDirectoryW is used to adjust .dll searches
			// on Linux, LD_LIBRARY_PATH is set to adjust .so searches
			_p = OSTailoredCode.LinkedLibManager.LoadOrThrow(dllName);
			HasLimitedLifetime = hasLimitedLifetime;
			if (!hasLimitedLifetime) GC.SuppressFinalize(this);
		}

		public IntPtr GetProcAddrOrZero(string entryPoint) => OSTailoredCode.LinkedLibManager.GetProcAddrOrZero(_p, entryPoint);

		public IntPtr GetProcAddrOrThrow(string entryPoint) => OSTailoredCode.LinkedLibManager.GetProcAddrOrThrow(_p, entryPoint);

		private void DisposeHelper()
		{
			if (!HasLimitedLifetime || _p == IntPtr.Zero) return;
			OSTailoredCode.LinkedLibManager.FreeByPtr(_p);
			_p = IntPtr.Zero;
		}

		public void Dispose()
		{
			DisposeHelper();
			GC.SuppressFinalize(this);
		}

		public IntPtr GetHandle() => _p;

		~DynamicLibraryImportResolver()
		{
			DisposeHelper();
		}
	}
}
