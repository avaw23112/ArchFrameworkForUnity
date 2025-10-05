using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Arch.Tools
{
	public class DotNetAssemblyLoader : IAssemblyLoader
	{
		private readonly string _pluginDirectory;

		public DotNetAssemblyLoader(string pluginDirectory = "Plugins")
		{
			_pluginDirectory = pluginDirectory;
		}

		public IEnumerable<Assembly> LoadAssemblies()
		{
			var pTaskLoading = LoadAssembliesAsync();
			pTaskLoading.Start();
			return pTaskLoading.Result;
		}

		public async Task<IEnumerable<Assembly>> LoadAssembliesAsync()
		{
			var result = new List<Assembly>();
			if (!Directory.Exists(_pluginDirectory))
				Directory.CreateDirectory(_pluginDirectory);

			var dlls = Directory.GetFiles(_pluginDirectory, "*.dll");
			foreach (var file in dlls)
			{
				try
				{
					var bytes = await File.ReadAllBytesAsync(file);
					var asm = Assembly.Load(bytes);
					result.Add(asm);
					ArchLog.LogInfo($"Loaded assembly: {Path.GetFileName(file)}");
				}
				catch (Exception ex)
				{
					ArchLog.LogWarning($"Failed to load {file}: {ex.Message}");
				}
			}
			return result;
		}

		public void RegisterAssembly(Assembly asm)
		{
			ArchLog.LogInfo($"Registered runtime assembly: {asm.FullName}");
		}

		public IEnumerable<Assembly> GetAllAssemblies() => AppDomain.CurrentDomain.GetAssemblies();
	}
}