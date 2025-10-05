using Arch;
using Arch.Tools;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Attributes
{
	public class Assemblys : Singleton<Assemblys>
	{
		public const string ASSEMBLY_NAME = "Assembly-CSharp";
		public const string AOT_ASSEMBLY_LABEL = "AOTdll";
		public const string HOTFIX_ASSEMBLY_LABEL = "Hotfixdll";
		public const string AOT_ASSEMBLY = "AOT";
		public const string HOTFIX_ASSEMBLY = "Logic";
		public const string MODEL_ASSEMBLY = "Data";

		private Dictionary<string, Assembly> m_dicAssemblys;
		public static IEnumerable<Assembly> AllAssemblies => Instance.m_dicAssemblys.Values;

		public Assemblys()
		{
			m_dicAssemblys = new Dictionary<string, Assembly>();
		}

		public static void LoadHotAssembly(Assembly hotReload)
		{
			//TODO:只会成功一次
			Instance.m_dicAssemblys.Remove(hotReload.GetName().Name);
			Instance.m_dicAssemblys.Add(hotReload.GetName().Name, hotReload);
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

			var AOTDll = await ArchRes.LoadAllByLabelAsync<TextAsset>(AOT_ASSEMBLY_LABEL);
			var HotfixDll = await ArchRes.LoadAllByLabelAsync<TextAsset>(HOTFIX_ASSEMBLY_LABEL);

#if !UNITY_EDITOR
			try
			{
				HomologousImageMode mode = HomologousImageMode.SuperSet;
				foreach (var aotDll in AOTDll)
				{
					LoadImageErrorCode err = HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(aotDll.bytes, HomologousImageMode.SuperSet);
					ArchLog.LogInfo($"LoadMetadataForAOTAssembly:{aotDll.name}. mode:{mode} ret:{err}");
				}
				var AOTdll = Assembly.Load(AOT_ASSEMBLY);
				Instance.m_dicAssemblys.TryAdd(AOTdll.FullName, AOTdll);

				foreach (var hotfixdll in HotfixDll)
				{
					var assembly = Assembly.Load(hotfixdll.bytes);
					Instance.m_dicAssemblys.TryAdd(assembly.FullName, assembly);
				}
			}
			catch (Exception e)
			{
				StringBuilder errorInfo = new StringBuilder();
				errorInfo.AppendLine($"程序集加载失败: [{e.GetType().Name}] {e.Message}");

				// 添加资源加载状态
				errorInfo.AppendLine($"AOT标签: {AOT_ASSEMBLY_LABEL}");
				errorInfo.AppendLine($"热更新标签: {HOTFIX_ASSEMBLY_LABEL}");

				// 遍历加载器上下文
				if (AOTDll != null)
				{
					errorInfo.AppendLine($"已加载AOT程序集数量: {AOTDll.Count}");
					foreach (var asset in AOTDll)
					{
						errorInfo.AppendLine($"- {asset.name} 大小: {asset.bytes.Length} bytes");
					}
				}

				if (HotfixDll != null)
				{
					errorInfo.AppendLine($"已加载热更新程序集数量: {HotfixDll.Count}");
					foreach (var asset in HotfixDll)
					{
						errorInfo.AppendLine($"- {asset.name} 大小: {asset.bytes.Length} bytes");
					}
				}

				// 添加完整的异常链
				Exception current = e;
				int level = 0;
				while (current != null)
				{
					errorInfo.AppendLine($"[异常层级 {level++}]");
					errorInfo.AppendLine($"类型: {current.GetType().FullName}");
					errorInfo.AppendLine($"消息: {current.Message}");
					errorInfo.AppendLine($"堆栈跟踪:\n{current.StackTrace}");
					current = current.InnerException;
				}

				// 添加程序集加载上下文
				try
				{
					errorInfo.AppendLine("\n当前已加载程序集:");
					foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
					{
						errorInfo.AppendLine($"- {asm.FullName} Location: {asm.Location}");
					}
				}
				catch (Exception asmEx)
				{
					errorInfo.AppendLine($"获取程序集列表失败: {asmEx.Message}");
				}

				ArchLog.LogError(errorInfo.ToString());
			}
#else
			string[] assembliesToLoad = { AOT_ASSEMBLY, MODEL_ASSEMBLY, HOTFIX_ASSEMBLY };
			foreach (var assemblyName in assembliesToLoad)
			{
				try
				{
					var assembly = Assembly.Load(assemblyName);
					Instance.m_dicAssemblys.TryAdd(assembly.GetName().Name, assembly);
				}
				catch (Exception ex)
				{
					ArchLog.LogError($"Editor :加载程序集 {assemblyName} 时出错: {ex.Message}");
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