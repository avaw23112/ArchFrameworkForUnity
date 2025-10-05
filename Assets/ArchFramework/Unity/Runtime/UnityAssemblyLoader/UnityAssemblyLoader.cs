#if UNITY_2020_1_OR_NEWER

using Arch.Resource;
using HybridCLR;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Arch.Tools
{
	public class UnityAssemblyLoader : IAssemblyLoader
	{
		private readonly string _aotLabel;
		private readonly string _hotfixLabel;

		public UnityAssemblyLoader(string aotLabel = "AOTdll", string hotfixLabel = "Hotfixdll")
		{
			_aotLabel = aotLabel;
			_hotfixLabel = hotfixLabel;
		}

		public IEnumerable<Assembly> LoadAssemblies()
		{
			var result = new List<Assembly>();
			try
			{
				var aotDlls = ArchRes.LoadAllByLabel<TextAsset>(_aotLabel);
				var hotfixDlls = ArchRes.LoadAllByLabel<TextAsset>(_hotfixLabel);
				foreach (var aot in aotDlls)
				{
					var err = HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(aot.bytes, HomologousImageMode.SuperSet);
					ArchLog.LogInfo($"Loaded AOT metadata: {aot.name}, result: {err}");
				}
				foreach (var dll in hotfixDlls)
				{
					var asm = Assembly.Load(dll.bytes);
					result.Add(asm);
					ArchLog.LogInfo($"Loaded Hotfix: {dll.name}");
				}
			}
			catch (Exception ex)
			{
				ArchLog.LogError($"UnityAssemblyLoader failed: {ex}");
			}

			return result;
		}

		public async Task<IEnumerable<Assembly>> LoadAssembliesAsync()
		{
			var result = new List<Assembly>();

			try
			{
				var aotDlls = await ArchRes.LoadAllByLabelAsync<TextAsset>(_aotLabel);
				var hotfixDlls = await ArchRes.LoadAllByLabelAsync<TextAsset>(_hotfixLabel);

				foreach (var aot in aotDlls)
				{
					var err = HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(aot.bytes, HomologousImageMode.SuperSet);
					ArchLog.LogInfo($"Loaded AOT metadata: {aot.name}, result: {err}");
				}

				foreach (var dll in hotfixDlls)
				{
					var asm = Assembly.Load(dll.bytes);
					result.Add(asm);
					ArchLog.LogInfo($"Loaded Hotfix: {dll.name}");
				}
			}
			catch (Exception ex)
			{
				ArchLog.LogError($"UnityAssemblyLoader failed: {ex}");
			}

			return result;
		}

		public void RegisterAssembly(Assembly asm)
		{
			ArchLog.LogInfo($"Registered dynamic assembly: {asm.FullName}");
		}

		public IEnumerable<Assembly> GetAllAssemblies() => AppDomain.CurrentDomain.GetAssemblies();
	}
}

#endif