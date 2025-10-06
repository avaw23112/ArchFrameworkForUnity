#if UNITY_2020_1_OR_NEWER

using Arch.Resource;
using Arch.Runtime;
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

		private bool LoadAssembliesOnEditor(List<Assembly> result)
		{
#if UNITY_EDITOR
			var assemblies = new string[] { GameRoot.Setting.AOT, GameRoot.Setting.Model, GameRoot.Setting.Logic, GameRoot.Setting.Protocol };
			foreach (var dll in assemblies)
			{
				var asm = Assembly.Load(dll);
				result.Add(asm);
				ArchLog.LogInfo($"Loaded Hotfix: {asm.GetName()}");
			}
			return true;
#else
			return false;
#endif
		}

		public IEnumerable<Assembly> LoadAssemblies()
		{
			var result = new List<Assembly>();
			if (LoadAssembliesOnEditor(result))
			{
				return result;
			}

			try
			{
				var aotDlls = ArchRes.LoadAllByLabel<TextAsset>(_aotLabel);
				var hotfixDlls = ArchRes.LoadAllByLabel<TextAsset>(_hotfixLabel);
				foreach (var aot in aotDlls)
				{
					var err = HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(aot.bytes, HomologousImageMode.SuperSet);
					ArchLog.LogInfo($"Loaded AOT metadata: {aot.name}, result: {err}");
				}
				Assembly assembly = Assembly.Load(GameRoot.Setting.AOT);
				if (assembly != null)
				{
					result.Add(assembly);
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
			if (LoadAssembliesOnEditor(result))
			{
				return result;
			}

			try
			{
				var aotDlls = await ArchRes.LoadAllByLabelAsync<TextAsset>(_aotLabel);
				var hotfixDlls = await ArchRes.LoadAllByLabelAsync<TextAsset>(_hotfixLabel);

				foreach (var aot in aotDlls)
				{
					var err = HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(aot.bytes, HomologousImageMode.SuperSet);
					ArchLog.LogInfo($"Loaded AOT metadata: {aot.name}, result: {err}");
				}

				Assembly assembly = Assembly.Load(GameRoot.Setting.AOT);
				if (assembly != null)
				{
					result.Add(assembly);
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