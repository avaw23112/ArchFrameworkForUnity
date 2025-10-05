using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Arch.Tools
{
	public static class Assemblys
	{
		private static IAssemblyLoader _loader;
		private static readonly Dictionary<string, Assembly> _assemblies = new();

		public static void SetLoader(IAssemblyLoader loader)
		{
			_loader = loader ?? throw new ArgumentNullException(nameof(loader));
		}

		public static void LoadAssemblys()
		{
			if (_loader == null)
				throw new InvalidOperationException("No assembly loader set. Call SetLoader() first.");

			var loadedAssemblies = _loader.LoadAssemblies();
			foreach (var asm in loadedAssemblies)
				_assemblies[asm.GetName().Name] = asm;
		}

		public static async Task LoadAssemblysAsync()
		{
			if (_loader == null)
				throw new InvalidOperationException("No assembly loader set. Call SetLoader() first.");

			var loadedAssemblies = await _loader.LoadAssembliesAsync();
			foreach (var asm in loadedAssemblies)
				_assemblies[asm.GetName().Name] = asm;
		}

		public static Assembly Get(string name)
			=> _assemblies.TryGetValue(name, out var asm) ? asm : null;

		public static bool Remove(string name) => _assemblies.Remove(name);

		public static IEnumerable<Assembly> All => _assemblies.Values;

		public static void Register(Assembly asm)
		{
			_loader?.RegisterAssembly(asm);
			_assemblies[asm.GetName().Name] = asm;
		}
	}
}