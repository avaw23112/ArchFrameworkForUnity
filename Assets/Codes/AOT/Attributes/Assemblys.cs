using Arch;
using Arch.Tools;
using Cysharp.Threading.Tasks;
using HybridCLR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;


namespace Attributes
{
	public class Assemblys : Singleton<Assemblys>
	{
		public const string ASSEMBLY_NAME = "Assembly-CSharp";
		public const string AOT_ASSEMBLY_LABEL = "AOTdll";
		public const string HOTFIX_ASSEMBLY_LABEL = "Hotfixdll";
		public const string AOT_ASSEMBLY = "AOT";
		public const string HOTFIX_ASSEMBLY = "Hotfix";

		private ConcurrentDictionary<string, Assembly> m_dicAssemblys;
		public static IEnumerable<Assembly> AllAssemblies => Instance.m_dicAssemblys.Values;

		public Assemblys()
		{
			m_dicAssemblys = new ConcurrentDictionary<string, Assembly>();
		}

		public static async UniTask LoadAssemblys()
		{
			if (Instance.m_dicAssemblys == null)
			{
				throw new Exception("程序集字典为空！");
			}
			if (Instance.m_dicAssemblys.Count > 0)
			{
				return;
			}

#if !UNITY_EDITOR
			try
			{
				var AOTdll = Assembly.Load(AOT_ASSEMBLY);
				Instance.m_dicAssemblys.TryAdd(AOTdll.FullName, AOTdll);
				var AOTDll = await ArchRes.LoadAllByLabelAsync<TextAsset>(AOT_ASSEMBLY_LABEL);
				foreach (var aotDll in AOTDll)
				{
					HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(aotDll.bytes, HomologousImageMode.SuperSet);
				}
				var HotfixDll = await ArchRes.LoadAllByLabelAsync<TextAsset>(HOTFIX_ASSEMBLY_LABEL);
				foreach (var hotfixdll in HotfixDll)
				{
					var assembly = Assembly.Load(hotfixdll.bytes);
					Instance.m_dicAssemblys.TryAdd(assembly.FullName, assembly);
				}
			}
			catch (Exception e)
			{
				ArchLog.Error($"加载程序集时出错: {e.Message}");
			}
#else
			string[] assembliesToLoad = { AOT_ASSEMBLY, HOTFIX_ASSEMBLY };
			foreach (var assemblyName in assembliesToLoad)
			{
				try
				{
					var assembly = Assembly.Load(assemblyName);
					Instance.m_dicAssemblys.TryAdd(assembly.FullName, assembly);
				}
				catch (Exception ex)
				{
					ArchLog.Error($"加载程序集 {assemblyName} 时出错: {ex.Message}");
				}
			}
			await UniTask.CompletedTask;
#endif
		}

		public static Assembly GetMainAssembly()
		{
			if (Instance.m_dicAssemblys == null)
			{
				throw new Exception("程序集字典为空！");
			}
			Assembly assembly = null;
			if (!Instance.m_dicAssemblys.TryGetValue(ASSEMBLY_NAME, out assembly))
			{
				throw new Exception("找不到主程序集，或未加载程序集！");
			}
			return assembly;
		}

		public static Assembly GetAssembly(string assemblyName)
		{
			if (Instance.m_dicAssemblys == null)
			{
				throw new Exception("程序集字典为空！");
			}
			Assembly assembly = null;
			if (!Instance.m_dicAssemblys.TryGetValue(assemblyName, out assembly))
			{
				throw new Exception("找不到目标程序集，或未加载程序集！");
			}
			return assembly;
		}

		public static Assembly GetCallingAssembly()
		{
			return Assembly.GetCallingAssembly();
		}

		public static Assembly GetEntryAssembly()
		{
			return Assembly.GetEntryAssembly();
		}

		public static Assembly GetExecutingAssembly()
		{
			return Assembly.GetExecutingAssembly();
		}
	}
}