using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using Arch.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
#endif

namespace Arch
{
	/// <summary>
	/// 资源名称→Address映射表（简化层级：根组→一级子组→资源）
	/// </summary>
	[CreateAssetMenu(fileName = "ResourceNameMap", menuName = "Arch/ResourceNameMap")]
	public class ResourceNameMap : ScriptableObject
	{
		#region 简化版层级结构（仅两层分组）
		/// <summary>
		/// 根组（仅对应Addressables的顶层Group，无更深嵌套）
		/// </summary>
		[SerializeField]
		[Tooltip("简化层级：根组（Addressables Group）→ 一级子组 → 资源")]
		protected List<ResourceRootGroup> _rootGroups = new List<ResourceRootGroup>();

		/// <summary>
		/// 根组类（对应Addressables的Group，仅包含一级子组）
		/// </summary>
		[Serializable]
		public class ResourceRootGroup
		{
			[Tooltip("根组名称（与Addressables Group名一致）")]
			public string groupName;

			[Tooltip("一级子组（对应资源的「一级路径」，无更深嵌套）")]
			public List<ResourceSubGroup> subGroups = new List<ResourceSubGroup>();
		}

		/// <summary>
		/// 一级子组类（对应资源的一级路径，直接包含资源）
		/// </summary>
		[Serializable]
		public class ResourceSubGroup
		{
			[Tooltip("子组名称（资源的一级路径，如「Prefabs」「Icons」）")]
			public string subGroupName;

			[Tooltip("子组下的所有资源（直接挂载，无更深层级）")]
			public List<ResourceMapping> resources = new List<ResourceMapping>();
		}

		/// <summary>
		/// 单个资源映射（结构不变，仅挂载层级简化）
		/// </summary>
		[Serializable]
		public class ResourceMapping
		{
			[Tooltip("资源名称（全局唯一，用于加载）")]
			public string resourceName;

			[Tooltip("Addressables实际Address")]
			public string address;

			[Tooltip("资源在项目中的路径（仅供预览）")]
			public string assetPath;
		}
		#endregion

		#region 运行时查询字典（逻辑不变，确保兼容）
		[NonSerialized] private Dictionary<string, string> _nameToAddress;
		[NonSerialized] private bool _isInitialized;

		public Dictionary<string, string> NameToAddress
		{
			get
			{
				if (!_isInitialized) Initialize();
				return _nameToAddress;
			}
		}
		#endregion

		#region 生命周期与初始化（适配简化层级）

		public void Initialize()
		{
			if (_isInitialized && _nameToAddress != null) return;

			try
			{
				_nameToAddress = new Dictionary<string, string>();
				if (_rootGroups == null)
				{
					_rootGroups = new List<ResourceRootGroup>();
					Debug.LogWarning($"[{name}] 根组列表为空，已初始化空列表");
					return;
				}

				// 遍历简化层级：根组 → 一级子组 → 资源
				foreach (var rootGroup in _rootGroups)
				{
					if (rootGroup?.subGroups == null) continue;

					foreach (var subGroup in rootGroup.subGroups)
					{
						if (subGroup?.resources == null) continue;

						foreach (var resource in subGroup.resources)
						{
							if (string.IsNullOrEmpty(resource?.resourceName) || string.IsNullOrEmpty(resource.address))
							{
								Debug.LogWarning($"[{name}] 跳过无效资源（{rootGroup.groupName}/{subGroup.subGroupName}）：{resource?.resourceName ?? "空名称"}");
								continue;
							}

							if (_nameToAddress.ContainsKey(resource.resourceName))
							{
								Debug.LogWarning($"[{name}] 资源名重复，覆盖旧映射：{resource.resourceName}");
								_nameToAddress[resource.resourceName] = resource.address;
							}
							else
							{
								_nameToAddress.Add(resource.resourceName, resource.address);
							}
						}
					}
				}

				_isInitialized = true;
				Debug.Log($"[{name}] 初始化完成，加载 {_nameToAddress.Count} 条有效映射");
			}
			catch (Exception e)
			{
				Debug.LogError($"[{name}] 初始化失败：{e.Message}\n{e.StackTrace}");
				_isInitialized = false;
				_nameToAddress = new Dictionary<string, string>();
			}
		}
		#endregion

		#region 对外查询接口（完全兼容旧逻辑）
		public bool TryGetAddress(string resourceName, out string address)
		{
			if (!_isInitialized) Initialize();
			if (string.IsNullOrEmpty(resourceName))
			{
				address = null;
				Debug.LogWarning($"[{name}] 传入空资源名，查询失败");
				return false;
			}

			bool success = _nameToAddress.TryGetValue(resourceName, out address);
			if (!success) Debug.LogWarning($"[{name}] 未找到资源：{resourceName}");
			return success;
		}

		public List<ResourceMapping> GetAllMappings()
		{
			var allResources = new List<ResourceMapping>();
			if (_rootGroups == null) return allResources;

			foreach (var rootGroup in _rootGroups)
			{
				if (rootGroup?.subGroups == null) continue;
				foreach (var subGroup in rootGroup.subGroups)
				{
					if (subGroup?.resources != null)
						allResources.AddRange(subGroup.resources);
				}
			}
			return allResources;
		}
		#endregion

		#region 简化版层级更新（与生成器联动核心）
#if UNITY_EDITOR
		/// <summary>
		/// 接收生成器数据，构建简化层级（根组→一级子组→资源）
		/// </summary>
		public void UpdateHierarchicalMappings(Dictionary<string, ResourceNameMapGenerator.ResourceMappingData> mappingDataDict)
		{
			if (mappingDataDict == null)
			{
				Debug.LogError($"[{name}] 传入空数据字典，更新失败");
				return;
			}

			try
			{
				// 1. 清空旧层级
				ClearHierarchy();

				foreach (var kvp in mappingDataDict)
				{
					string resourceName = kvp.Key;
					var data = kvp.Value;

					if (string.IsNullOrEmpty(resourceName) || string.IsNullOrEmpty(data.address))
					{
						Debug.LogWarning($"[{name}] 跳过无效数据：{resourceName ?? "空名称"}");
						continue;
					}

					// 2. 找到/创建 根组（对应Addressables Group）
					ResourceRootGroup rootGroup = GetOrCreateRootGroup(data.groupName);

					// 3. 提取「一级子路径」（核心简化：仅取路径第一级）
					string firstLevelPath = GetFirstLevelPath(data.subPath);
					// 无路径时用默认子组名，避免根组下直接堆资源
					string subGroupName = string.IsNullOrEmpty(firstLevelPath) ? "默认资源" : firstLevelPath;

					// 4. 找到/创建 一级子组（仅一层，不嵌套）
					ResourceSubGroup subGroup = GetOrCreateSubGroup(rootGroup, subGroupName);

					// 5. 添加资源到子组（直接挂载，无更深层级）
					subGroup.resources.Add(new ResourceMapping
					{
						resourceName = resourceName,
						address = data.address,
						assetPath = data.assetPath ?? ""
					});
				}

				// 6. 排序（根组→子组→资源按名称排序，保持整洁）
				SortSimplifiedHierarchy();

				// 7. 重新初始化字典
				_isInitialized = false;
				Initialize();

				Debug.Log($"[{name}] 简化层级更新完成：{_rootGroups.Count} 个根组，{GetTotalResourceCount()} 个资源");
			}
			catch (Exception e)
			{
				Debug.LogError($"[{name}] 更新层级失败：{e.Message}\n{e.StackTrace}");
				ClearHierarchy();
			}
		}
#endif
		/// <summary>
		/// 核心简化：从多级路径中提取「一级路径」（如"UI/Prefabs/Button" → "Prefabs"）
		/// </summary>
		private string GetFirstLevelPath(string fullSubPath)
		{
			if (string.IsNullOrEmpty(fullSubPath)) return "";

			// 拆分路径（按"/"分割）
			string[] pathParts = fullSubPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			// 仅返回第一级路径（无多级则返回空，后续会归为"默认资源"）
			return pathParts.Length > 0 ? pathParts[0] : "";
		}

		/// <summary>
		/// 找到/创建 根组（仅对应Addressables Group）
		/// </summary>
		private ResourceRootGroup GetOrCreateRootGroup(string groupName)
		{
			string safeName = string.IsNullOrEmpty(groupName) ? "未命名Group" : groupName;
			var existing = _rootGroups.Find(g => g.groupName == safeName);

			if (existing == null)
			{
				existing = new ResourceRootGroup { groupName = safeName };
				_rootGroups.Add(existing);
			}
			return existing;
		}

		/// <summary>
		/// 找到/创建 一级子组（仅一层，不嵌套）
		/// </summary>
		private ResourceSubGroup GetOrCreateSubGroup(ResourceRootGroup rootGroup, string subGroupName)
		{
			if (rootGroup == null)
			{
				rootGroup = new ResourceRootGroup { groupName = "临时Group" };
				_rootGroups.Add(rootGroup);
			}

			// 初始化子组列表（避免空引用）
			if (rootGroup.subGroups == null)
				rootGroup.subGroups = new List<ResourceSubGroup>();

			var existing = rootGroup.subGroups.Find(sg => sg.subGroupName == subGroupName);
			if (existing == null)
			{
				existing = new ResourceSubGroup { subGroupName = subGroupName };
				rootGroup.subGroups.Add(existing);
			}
			return existing;
		}

		/// <summary>
		/// 简化层级排序（根组→子组→资源均按名称升序）
		/// </summary>
		private void SortSimplifiedHierarchy()
		{
			// 排序根组
			_rootGroups?.Sort((a, b) => string.Compare(a.groupName, b.groupName));

			// 排序每个根组下的子组和资源
			foreach (var rootGroup in _rootGroups ?? new List<ResourceRootGroup>())
			{
				// 排序一级子组
				rootGroup.subGroups?.Sort((a, b) => string.Compare(a.subGroupName, b.subGroupName));

				// 排序每个子组下的资源
				foreach (var subGroup in rootGroup.subGroups ?? new List<ResourceSubGroup>())
				{
					subGroup.resources?.Sort((a, b) => string.Compare(a.resourceName, b.resourceName));
				}
			}
		}

		/// <summary>
		/// 清空层级（简化版）
		/// </summary>
		private void ClearHierarchy()
		{
			if (_rootGroups != null)
				_rootGroups.Clear();
			else
				_rootGroups = new List<ResourceRootGroup>();
		}

		/// <summary>
		/// 统计资源总数（简化版）
		/// </summary>
		private int GetTotalResourceCount()
		{
			int count = 0;
			foreach (var rootGroup in _rootGroups ?? new List<ResourceRootGroup>())
			{
				if (rootGroup?.subGroups == null) continue;
				foreach (var subGroup in rootGroup.subGroups)
				{
					count += subGroup?.resources?.Count ?? 0;
				}
			}
			return count;
		}
		#endregion

		#region Editor辅助功能（简化后适配）
		[ContextMenu("刷新层级与字典")]
		public void RefreshHierarchyAndDict()
		{
#if UNITY_EDITOR
			SortSimplifiedHierarchy();
			_isInitialized = false;
			Initialize();
			EditorUtility.SetDirty(this);
			Debug.Log($"[{name}] 手动刷新完成（简化层级）");
#endif
		}

		[ContextMenu("清空所有数据")]
		public void ClearAllData()
		{
#if UNITY_EDITOR
			ClearHierarchy();
			_nameToAddress?.Clear();
			_isInitialized = false;
			EditorUtility.SetDirty(this);
			Debug.Log($"[{name}] 所有数据已清空");
#endif
		}

		/// <summary>
		/// Editor下反推资源路径（保持不变）
		/// </summary>
		public string GetAssetPathFromAddress(string address)
		{
#if UNITY_EDITOR
			if (string.IsNullOrEmpty(address)) return "";
			var settings = AddressableAssetSettingsDefaultObject.Settings;
			return settings?.FindAssetEntry(address)?.AssetPath ?? "";
#else
			return "";
#endif
		}
		#endregion
	}
}
