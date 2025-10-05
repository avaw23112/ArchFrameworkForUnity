#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;


namespace Arch.Editor
{
	/// <summary>
	/// 资源名称→Address映射表生成器（支持Addressables分层结构）
	/// </summary>
	public static class ResourceNameMapGenerator
	{
		// 映射表保存路径（Assets下）
		private const string MapAssetPath = "Assets/Resources/ResourceNameMap.asset";

		// 【新增】存储资源完整分层信息的结构体（传递给SO用于构建层级）
		public struct ResourceMappingData
		{
			public string address;       // 资源Address
			public string groupName;     // 所属Addressables组名
			public string subPath;       // 组内子路径（如"UI/Prefabs"，用于层级拆分）
			public string assetPath;     // 本地资源路径（预览用）
		}

		[MenuItem("Tools/生成资源名称映射表 _F3")]
		public static void GenerateResourceNameMap()
		{
			var settings = AddressableAssetSettingsDefaultObject.Settings;
			if (settings == null)
			{
				Debug.LogError("Addressable设置不存在");
				return;
			}

			// 1. 收集资源：Key=资源名，Value=完整分层信息（替换原仅存Address的字典）
			var resourceMap = new Dictionary<string, ResourceMappingData>();

			// 收集独立资源（有Entry的资源）
			CollectIndependentResources(settings, resourceMap);

			// 收集集中打包的子资源（无Entry）
			CollectPackedFolderResources(settings, resourceMap);

			// 2. 检测重复名称（仍按资源名全局唯一，避免加载冲突）
			if (HasDuplicateNames(resourceMap))
			{
				Debug.LogError("存在重复资源名称，映射表生成失败");
				return;
			}

			// 3. 保存到SO（传递分层数据）
			SaveResourceMap(resourceMap);

			AssetDatabase.Refresh();
			Debug.Log($"资源名称映射表生成成功：共收录 {resourceMap.Count} 个资源，分层结构已对齐Addressables");
		}

		/// <summary>
		/// 收集独立资源（有Entry的资源）→ 提取组名+子路径
		/// </summary>
		private static void CollectIndependentResources(
			AddressableAssetSettings settings,
			Dictionary<string, ResourceMappingData> resourceMap)
		{
			foreach (var group in settings.groups)
			{
				// 跳过默认分组、Built In Data和空分组（保持原逻辑）
				if (group.Default || group.Name == "Built In Data" || group.entries.Count == 0)
					continue;

				string groupName = group.Name; // 记录当前资源所属的Addressables组名

				foreach (var entry in group.entries)
				{
					if (entry.IsFolder) continue; // 跳过文件夹Entry

					// 基础信息
					string assetPath = entry.AssetPath;
					string resourceName = GetResourceName(assetPath);
					string address = entry.address;

					// 【关键】拆分组内子路径（如address="UI/Prefabs/Button" → subPath="UI/Prefabs"）
					string subPath = GetSubPathFromAddress(address);

					// 组装分层数据并加入映射表
					var mappingData = new ResourceMappingData
					{
						address = address,
						groupName = groupName,
						subPath = subPath,
						assetPath = assetPath
					};
					AddToMap(resourceMap, resourceName, mappingData);
				}
			}
		}

		/// <summary>
		/// 收集集中打包的子资源（无Entry）→ 提取组名+子路径
		/// </summary>
		private static void CollectPackedFolderResources(
			AddressableAssetSettings settings,
			Dictionary<string, ResourceMappingData> resourceMap)
		{
			// 找到所有非默认组的Addressable文件夹
			var packedFolders = new List<AddressableAssetEntry>();
			foreach (var group in settings.groups)
			{
				if (group.Default || group.Name == "Built In Data")
					continue;

				packedFolders.AddRange(group.entries
					.Where(e => e.IsFolder)
					.ToList());
			}

			foreach (var folderEntry in packedFolders)
			{
				string folderPath = folderEntry.AssetPath;
				if (!Directory.Exists(folderPath)) continue;

				// 基础信息：文件夹的组名、Address
				string groupName = folderEntry.parentGroup.Name;
				string folderAddress = folderEntry.address.TrimEnd('/');

				// 遍历文件夹下所有子资源
				foreach (var assetPath in Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories))
				{
					if (assetPath.EndsWith(".meta") || Directory.Exists(assetPath)) continue;

					// 基础信息
					string resourceName = GetResourceName(assetPath);
					string relativePath = Path.GetRelativePath(folderPath, assetPath).Replace("\\", "/");
					string address = $"{folderAddress}/{relativePath}".Replace("//", "/");

					// 【关键】拆分组内子路径（如address="Res/Prefabs/Player" → subPath="Res/Prefabs"）
					string subPath = GetSubPathFromAddress(address);

					// 组装分层数据并加入映射表
					var mappingData = new ResourceMappingData
					{
						address = address,
						groupName = groupName,
						subPath = subPath,
						assetPath = assetPath
					};
					AddToMap(resourceMap, resourceName, mappingData);
				}
			}
		}

		/// <summary>
		/// 【新增】从Address中拆分「组内子路径」（如"UI/Icon/Close" → "UI/Icon"）
		/// </summary>
		private static string GetSubPathFromAddress(string address)
		{
			if (string.IsNullOrEmpty(address))
				return string.Empty;

			// 取Address的目录部分（去掉最后一级资源名）
			string addressDir = Path.GetDirectoryName(address).Replace("\\", "/");
			// 处理根路径场景（如address="Player" → 子路径为空）
			return string.IsNullOrEmpty(addressDir) ? string.Empty : addressDir;
		}

		/// <summary>
		/// 提取资源名称（保持原逻辑：文件名不含扩展名）
		/// </summary>
		private static string GetResourceName(string assetPath)
		{
			return Path.GetFileNameWithoutExtension(assetPath);
		}

		/// <summary>
		/// 【修改】添加分层数据到映射表（带重复检测）
		/// </summary>
		private static void AddToMap(
			Dictionary<string, ResourceMappingData> map,
			string resourceName,
			ResourceMappingData mappingData)
		{
			if (map.TryGetValue(resourceName, out var existingData))
			{
				// 若同一资源名对应不同Address，抛警告（全局资源名需唯一）
				if (existingData.address != mappingData.address)
				{
					Debug.LogWarning(
						$"资源名称重复：{resourceName}\n" +
						$"1. 组={existingData.groupName} | 地址={existingData.address} | 路径={existingData.assetPath}\n" +
						$"2. 组={mappingData.groupName} | 地址={mappingData.address} | 路径={mappingData.assetPath}");
				}
			}
			else
			{
				map[resourceName] = mappingData;
			}
		}

		/// <summary>
		/// 【修改】检测重复资源名（基于新的分层数据字典）
		/// </summary>
		private static bool HasDuplicateNames(Dictionary<string, ResourceMappingData> map)
		{
			// 按资源名分组，若存在组内数量>1则视为重复
			return map.GroupBy(kv => kv.Key)
					  .Any(group => group.Count() > 1);
		}

		/// <summary>
		/// 【关键修改】保存分层数据到SO（调用SO的层级构建方法）
		/// </summary>
		private static void SaveResourceMap(Dictionary<string, ResourceMappingData> resourceMap)
		{
			// 创建或获取SO实例
			ResourceNameMap mapAsset = AssetDatabase.LoadAssetAtPath<ResourceNameMap>(MapAssetPath);
			if (mapAsset == null)
			{
				string directory = Path.GetDirectoryName(MapAssetPath);
				if (!Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory);
				}

				mapAsset = ScriptableObject.CreateInstance<ResourceNameMap>();
				AssetDatabase.CreateAsset(mapAsset, MapAssetPath);
			}

			// 调用SO的「分层更新方法」（需在SO侧同步实现）
			// 区别于原扁平更新，此处传递完整分层数据
			mapAsset.UpdateHierarchicalMappings(resourceMap);

			// 标记脏数据并保存
			EditorUtility.SetDirty(mapAsset);
			AssetDatabase.SaveAssets();
		}
	}
}
#endif
