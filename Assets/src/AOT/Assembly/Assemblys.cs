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
			//TODO:ֻ��ɹ�һ��
			Instance.m_dicAssemblys.Remove(hotReload.GetName().Name);
			Instance.m_dicAssemblys.Add(hotReload.GetName().Name, hotReload);
		}

		public static async UniTask LoadAssemblys()
		{
			if (Instance.m_dicAssemblys == null)
			{
				throw new Exception("�����ֵ�Ϊ�գ�");
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
				errorInfo.AppendLine($"���򼯼���ʧ��: [{e.GetType().Name}] {e.Message}");

				// �����Դ����״̬
				errorInfo.AppendLine($"AOT��ǩ: {AOT_ASSEMBLY_LABEL}");
				errorInfo.AppendLine($"�ȸ��±�ǩ: {HOTFIX_ASSEMBLY_LABEL}");

				// ����������������
				if (AOTDll != null)
				{
					errorInfo.AppendLine($"�Ѽ���AOT��������: {AOTDll.Count}");
					foreach (var asset in AOTDll)
					{
						errorInfo.AppendLine($"- {asset.name} ��С: {asset.bytes.Length} bytes");
					}
				}

				if (HotfixDll != null)
				{
					errorInfo.AppendLine($"�Ѽ����ȸ��³�������: {HotfixDll.Count}");
					foreach (var asset in HotfixDll)
					{
						errorInfo.AppendLine($"- {asset.name} ��С: {asset.bytes.Length} bytes");
					}
				}

				// ����������쳣��
				Exception current = e;
				int level = 0;
				while (current != null)
				{
					errorInfo.AppendLine($"[�쳣�㼶 {level++}]");
					errorInfo.AppendLine($"����: {current.GetType().FullName}");
					errorInfo.AppendLine($"��Ϣ: {current.Message}");
					errorInfo.AppendLine($"��ջ����:\n{current.StackTrace}");
					current = current.InnerException;
				}

				// ��ӳ��򼯼���������
				try
				{
					errorInfo.AppendLine("\n��ǰ�Ѽ��س���:");
					foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
					{
						errorInfo.AppendLine($"- {asm.FullName} Location: {asm.Location}");
					}
				}
				catch (Exception asmEx)
				{
					errorInfo.AppendLine($"��ȡ�����б�ʧ��: {asmEx.Message}");
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
					ArchLog.LogError($"Editor :���س��� {assemblyName} ʱ����: {ex.Message}");
				}
			}
			await UniTask.CompletedTask;
#endif
		}

		public static Assembly GetMainAssembly()
		{
			if (Instance.m_dicAssemblys == null)
			{
				throw new Exception("�����ֵ�Ϊ�գ�");
			}
			Assembly assembly = null;
			if (!Instance.m_dicAssemblys.TryGetValue(ASSEMBLY_NAME, out assembly))
			{
				throw new Exception("�Ҳ��������򼯣���δ���س��򼯣�");
			}
			return assembly;
		}

		public static Assembly GetAssembly(string assemblyName)
		{
			if (Instance.m_dicAssemblys == null)
			{
				throw new Exception("�����ֵ�Ϊ�գ�");
			}
			Assembly assembly = null;
			if (!Instance.m_dicAssemblys.TryGetValue(assemblyName, out assembly))
			{
				throw new Exception("�Ҳ���Ŀ����򼯣���δ���س��򼯣�");
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