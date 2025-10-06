#if UNITY_2020_1_OR_NEWER

using Arch.Compilation.Editor;
using Arch.Resource;
using Arch.Runtime;
using Cysharp.Threading.Tasks;
using HybridCLR;
using System;
using System.Collections.Generic;
using System.Linq;
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

		private bool LoadAssembliesOnEditor(List<Assembly> result, ArchBuildConfig archBuildConfig)
		{
#if UNITY_EDITOR
			string[] assemblies;
			if (archBuildConfig.buildSetting.buildMode == BuildSetting.AssemblyBuildMode.Isolated)
			{
				assemblies = new string[]
				{
					GameRoot.Setting.Logic,
					GameRoot.Setting.Model,
					GameRoot.Setting.AOT,
					GameRoot.Setting.Protocol,
				};
			}
			else
			{
				assemblies = new string[]
				{
					GameRoot.Setting.FullLink
				};
			}
			try
			{
				foreach (var dll in assemblies)
				{
					var asm = Assembly.Load(dll);
					result.Add(asm);
					ArchLog.LogInfo($"Loaded Hotfix: {asm.GetName()}");
				}
			}
			catch
			{
				ArchLog.LogError("Load assembly faild");
				throw;
			}

			return true;
#else
			return false;
#endif
		}

		public IEnumerable<Assembly> LoadAssemblies()
		{
			var result = new List<Assembly>();
			ArchBuildConfig archBuildConfig = ArchBuildConfig.LoadOrCreate();
			if (LoadAssembliesOnEditor(result, archBuildConfig))
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
				if (archBuildConfig.buildSetting.buildMode == BuildSetting.AssemblyBuildMode.FullLink
					&& hotfixDlls.Count() > 1)
				{
					ArchLog.LogError("Befor loading fullLink assembly,please delete all isolates");
					throw new Exception("Befor loading fullLink assembly,please delete all isolates");
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
			ArchBuildConfig archBuildConfig = ArchBuildConfig.LoadOrCreate();
			if (LoadAssembliesOnEditor(result, archBuildConfig))
			{
				return result;
			}
			await UniTask.SwitchToMainThread();
			try
			{
				var aotDlls = await ArchRes.LoadAllByLabelAsync<TextAsset>(_aotLabel);
				var hotfixDlls = await ArchRes.LoadAllByLabelAsync<TextAsset>(_hotfixLabel);

				foreach (var aot in aotDlls)
				{
					var err = HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(aot.bytes, HomologousImageMode.SuperSet);
					ArchLog.LogInfo($"Loaded AOT metadata: {aot.name}, result: {err}");
				}
				if (archBuildConfig.buildSetting.buildMode == BuildSetting.AssemblyBuildMode.FullLink
		&& hotfixDlls.Count() > 1)
				{
					ArchLog.LogError("Befor loading fullLink assembly,please delete all isolates");
					throw new Exception("Befor loading fullLink assembly,please delete all isolates");
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