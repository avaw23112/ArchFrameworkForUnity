using Arch.Tools;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations; // 必须添加该命名空间
namespace Arch
{
	/// <summary>
	/// 资源管理主入口（整合Addressables更新+加载+实例化+释放）
	/// </summary>
	public static partial class ArchRes
	{
		#region 核心状态变量（确保线程安全与状态唯一）
		/// <summary>
		/// Addressables是否初始化完成
		/// </summary>
		private static bool _isAddressablesInited;

		/// <summary>
		/// 资源映射表是否加载完成
		/// </summary>
		private static bool _isNameMapInited;

		/// <summary>
		/// 资源映射表实例
		/// </summary>
		private static ResourceNameMap _nameMap;

		/// <summary>
		/// 正在加载中的操作句柄（避免重复加载）
		/// </summary>
		private static readonly Dictionary<string, AsyncOperationHandle> _loadingHandles = new();

		/// <summary>
		/// 更新相关：待更新资源的Address→大小映射
		/// </summary>
		private static Dictionary<string, long> _updateSizeMap;

		/// <summary>
		/// 更新相关：是否有可用更新
		/// </summary>
		private static bool _hasAvailableUpdate;

		/// <summary>
		/// 更新相关：总更新大小（字节）
		/// </summary>
		private static long _totalUpdateSize;

		/// <summary>
		/// 初始化锁（避免多线程重复初始化）
		/// </summary>
		private static readonly object _initLock = new();

		/// <summary>
		/// 更新锁（避免多线程重复检测/下载更新）
		/// </summary>
		private static readonly object _updateLock = new();
		#endregion

		#region 初始化（整合Addressables+映射表，确保唯一初始化）
		/// <summary>
		/// 完整初始化资源管理器（Addressables初始化 + 映射表加载）
		/// </summary>
		/// <param name="onError">初始化失败回调（参数：错误信息）</param>
		public static async UniTask<bool> InitializeAsync(Action<string> onError = null)
		{
			// 已完成完整初始化，直接返回
			if (_isAddressablesInited && _isNameMapInited)
				return true;

			try
			{
				// 1. 先初始化Addressables（必须第一步，否则后续操作无效）
				if (!await InitAddressablesAsync(onError))
					return false;
				// 2. 再加载资源映射表（依赖Addressables）
				if (!LoadResourceNameMapAsync(onError))
					return false;
				_isNameMapInited = true;
				ArchLog.Debug($"ArchRes 完整初始化完成！");
				return true;
			}
			catch (Exception ex)
			{
				onError?.Invoke($"初始化异常：{ex.Message}\n{ex.StackTrace}");
				ArchLog.Error($"ArchRes InitializeAsync Exception: {ex}");
				return false;
			}
		}

		/// <summary>
		/// 单独初始化Addressables（内部调用，外部建议用InitializeAsync）
		/// </summary>
		private static async UniTask<bool> InitAddressablesAsync(Action<string> onError)
		{

			if (_isAddressablesInited)
				return true;

			// Addressables初始化（对应原脚本的InitializeAsync）
			var initHandle = Addressables.InitializeAsync();
			await initHandle;

			_isAddressablesInited = initHandle.Status == AsyncOperationStatus.Succeeded;

			if (!_isAddressablesInited)
			{
				string errorMsg = $"Addressables初始化失败：{initHandle.OperationException?.Message ?? "未知错误"}";
				onError?.Invoke(errorMsg);
				ArchLog.Error(errorMsg);
				// 释放失败的句柄
				Addressables.Release(initHandle);
			}

			return _isAddressablesInited;
		}

		/// <summary>
		/// 加载资源映射表（内部调用，依赖Addressables初始化）
		/// </summary>
		private static bool LoadResourceNameMapAsync(Action<string> onError)
		{
			if (_isNameMapInited || _nameMap != null)
				return true;

			// 从Addressables加载映射表（原逻辑是从Resources，这里统一用Addressables更规范）
			_nameMap = Resources.Load<ResourceNameMap>("ResourceNameMap");

			if (_nameMap == null)
			{
				string errorMsg = $"资源映射表加载失败： 映射表不存在，请先执行「Tools→生成资源名称映射表」";
				onError?.Invoke(errorMsg);
				ArchLog.Error(errorMsg);
				Resources.UnloadAsset(_nameMap);
				return false;
			}

			ArchLog.Debug($"资源映射表加载完成，包含 {_nameMap.GetAllMappings().Count} 条资源映射");
			return true;
		}
		#endregion
		/// <summary>
		/// 检测并下载资源更新（适配1.21.21，修正InvalidKeyException）
		/// </summary>
		public static async UniTask<bool> CheckForUpdatesAsync(string preloadLabel = "preload", Action<string> onError = null)
		{
			System.Threading.CancellationToken cancellationToken = new System.Threading.CancellationToken();

			try
			{
				// 1. 检测Catalog是否有更新（返回需要更新的Catalog列表）
				var catalogCheckHandle = Addressables.CheckForCatalogUpdates(false);
				await catalogCheckHandle;

				if (!catalogCheckHandle.IsValid() || catalogCheckHandle.Status != AsyncOperationStatus.Succeeded)
				{
					string error = $"Catalog检测失败：{catalogCheckHandle.OperationException?.Message}";
					onError?.Invoke(error);
					ArchLog.Error(error);
					Addressables.Release(catalogCheckHandle);
					return false;
				}

				// 2. 若有Catalog更新，先执行更新（否则后续资源定位可能过时）
				if (catalogCheckHandle.Result.Count > 0)
				{
					var catalogUpdateHandle = Addressables.UpdateCatalogs(catalogCheckHandle.Result, false);
					await catalogUpdateHandle;

					if (!catalogUpdateHandle.IsValid() || catalogUpdateHandle.Status != AsyncOperationStatus.Succeeded)
					{
						string error = $"Catalog更新失败：{catalogUpdateHandle.OperationException?.Message}";
						onError?.Invoke(error);
						ArchLog.Error(error);
						Addressables.Release(catalogUpdateHandle);
						Addressables.Release(catalogCheckHandle);
						return false;
					}

					ArchLog.Debug($"已更新{catalogUpdateHandle.Result.Count}个Catalog");
					Addressables.Release(catalogUpdateHandle);
				}

				// 3. 获取预加载资源的有效Key（关键：用资源Key而非Catalog列表
				var preloadKeys = await GetValidPreloadKeysAsync(preloadLabel);
				if (preloadKeys.Count == 0)
				{
					ArchLog.Debug($"预加载标签[{preloadLabel}]下无有效资源，无需更新");
					Addressables.Release(catalogCheckHandle);
					return true; // 无资源需更新，视为成功
				}

				// 4. 下载资源依赖（传入有效的资源Key，而非Catalog列表）
				var downloadHandle = Addressables.DownloadDependenciesAsync(
					keys: preloadKeys,
					mode: Addressables.MergeMode.Union
				).WithCancellation(cancellationToken);

				await downloadHandle;

				if (downloadHandle.Status.IsFaulted())
				{
					string error = $"资源依赖下载失败：{downloadHandle.Status}";
					onError?.Invoke(error);
					ArchLog.Error(error);
					Addressables.Release(downloadHandle);
					Addressables.Release(catalogCheckHandle);
					return false;
				}

				ArchLog.Debug($"成功载完成，共{preloadKeys.Count}个资源");
				Addressables.Release(downloadHandle);
				Addressables.Release(catalogCheckHandle);
				return true;
			}
			catch (InvalidKeyException ex)
			{
				// 专门门处理无效Key异常（通常是资源配置错误）
				string error = $"无效的资源Key：{ex.Message}，请检查资源配置";
				onError?.Invoke(error);
				ArchLog.Error(error);
				return false;
			}
			catch (Exception ex)
			{
				string error = $"更新检测异常：{ex.Message}\n{ex.StackTrace}";
				onError?.Invoke(error);
				ArchLog.Error(error);
				return false;
			}
		}

		/// <summary>
		/// 获取预加载标签下的有效资源Key（确保免无效Key导致的异常）
		/// </summary>
		private static async UniTask<List<string>> GetValidPreloadKeysAsync(string preloadLabel)
		{
			var validKeys = new List<string>();

			// 获取标签对应的资源定位
			var locsHandle = Addressables.LoadResourceLocationsAsync(preloadLabel);
			await locsHandle;

			if (locsHandle.IsValid() && locsHandle.Status == AsyncOperationStatus.Succeeded)
			{
				// 过滤无效定位，提取唯一Key
				foreach (var loc in locsHandle.Result)
				{
					if (loc != null && !string.IsNullOrEmpty(loc.PrimaryKey) && !validKeys.Contains(loc.PrimaryKey))
					{
						validKeys.Add(loc.PrimaryKey);
					}
				}
			}

			Addressables.Release(locsHandle);
			return validKeys;
		}



		/// <summary>
		/// 递归验证资源定位及其所有依赖项是否有效（无null）
		/// </summary>
		private static bool IsValidLocation(IResourceLocation loc)
		{
			// 基础验证：定位本身不为null且PrimaryKey有效
			if (loc == null || string.IsNullOrEmpty(loc.PrimaryKey) || loc.ResourceType == null)
				return false;

			// 递归验证所有依赖项
			if (loc.Dependencies != null)
			{
				foreach (var dep in loc.Dependencies)
				{
					// 若任何一个依赖无效，则当前定位无效
					if (!IsValidLocation(dep))
						return false;
				}
			}

			return true;
		}





		/// <summary>
		/// 下载所有检测到的更新资源
		/// </summary>
		/// <param name="progressCallback">下载进度回调（0~1）</param>
		/// <param name="onError">下载失败回调</param>
		/// <returns>下载是否成功</returns>
		public static async UniTask<bool> DownloadUpdatesAsync(Action<float> progressCallback = null, Action<string> onError = null)
		{
			lock (_updateLock)
			{
				// 无可用更新，直接返回成功
				if (!_hasAvailableUpdate || _updateSizeMap.Count == 0)
				{
					progressCallback?.Invoke(1f); // 进度置为100%
					ArchLog.Debug("无可用更新，无需下载");
					return true;
				}
			}

			// 确保Addressables已初始化
			if (!await InitAddressablesAsync(onError))
				return false;

			AsyncOperationHandle downloadHandle = default;
			try
			{
				// 1. 准备下载列表（从更新大小映射中获取待下载Address）
				var downloadKeys = new List<string>();
				lock (_updateLock)
				{
					downloadKeys.AddRange(_updateSizeMap.Keys);
				}

				ArchLog.Debug($"开始下载更新：{downloadKeys.Count} 个资源，总大小 {FormatFileSize(_totalUpdateSize)}");

				// 2. 执行下载（合并模式：Union，不自动释放句柄）
				downloadHandle = Addressables.DownloadDependenciesAsync(
					keys: downloadKeys,
					mode: Addressables.MergeMode.Union,
					autoReleaseHandle: false
				);

				// 3. 实时回调下载进度（对应原脚本的while循环）
				while (!downloadHandle.IsDone)
				{
					// 过滤无效进度（避免早期0%、后期超过100%的情况）
					float safeProgress = Mathf.Clamp01(downloadHandle.PercentComplete);
					progressCallback?.Invoke(safeProgress);
					await UniTask.Yield(); // 替代yield return null，UniTask更高效
				}

				// 4. 下载完成后进度置为100%
				progressCallback?.Invoke(1f);

				// 5. 检查下载结果
				if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
				{
					lock (_updateLock)
					{
						_hasAvailableUpdate = false; // 重置更新状态
						_updateSizeMap.Clear();
					}

					ArchLog.Debug("所有更新资源下载完成！");
					return true;
				}
				else
				{
					string errorMsg = $"下载失败：{downloadHandle.OperationException?.Message ?? "未知错误"}";
					onError?.Invoke(errorMsg);
					ArchLog.Error(errorMsg);
					return false;
				}
			}
			catch (Exception ex)
			{
				string errorMsg = $"下载异常：{ex.Message}\n{ex.StackTrace}";
				onError?.Invoke(errorMsg);
				ArchLog.Error(errorMsg);
				return false;
			}
			finally
			{
				// 无论成功失败，都释放下载句柄（避免内存泄漏）
				if (downloadHandle.IsValid())
					Addressables.Release(downloadHandle);
			}
		}

		/// <summary>
		/// 获取总更新大小（带单位格式化，如"1.2MB"）
		/// </summary>
		public static string GetFormattedTotalUpdateSize()
		{
			lock (_updateLock)
			{
				return FormatFileSize(_totalUpdateSize);
			}
		}

		/// <summary>
		/// 获取原始总更新大小（字节）
		/// </summary>
		public static long GetRawTotalUpdateSize()
		{
			lock (_updateLock)
			{
				return _totalUpdateSize;
			}
		}

		/// <summary>
		/// 工具方法：格式化文件大小（字节→KB/MB/GB）
		/// </summary>
		private static string FormatFileSize(long bytes)
		{
			if (bytes < 1024)
				return $"{bytes} B";
			else if (bytes < 1024 * 1024)
				return $"{(bytes / 1024f):F2} KB";
			else if (bytes < 1024 * 1024 * 1024)
				return $"{(bytes / (1024f * 1024f)):F2} MB";
			else
				return $"{(bytes / (1024f * 1024f * 1024f)):F2} GB";
		}

		/// <summary>
		/// 根据Addressables Label加载该标签下的所有资源（自动过滤无效资源）
		/// </summary>
		/// <typeparam name="T">资源类型（如Texture2D、GameObject、AudioClip等）</typeparam>
		/// <param name="label">目标Label（Addressables中配置的标签）</param>
		/// <param name="progressCallback">加载进度回调（参数1：已加载数量，参数2：总数量）</param>
		/// <param name="onLoaded">全部加载完成回调（返回成功加载的资源列表）</param>
		/// <param name="onError">加载失败回调（参数：错误信息）</param>
		/// <returns>成功加载的资源列表（空列表表示无资源或全部加载失败）</returns>
		public static async UniTask<List<T>> LoadAllByLabelAsync<T>(
			string label,
			Action<int, int> progressCallback = null,
			Action<List<T>> onLoaded = null,
			Action<string> onError = null) where T : UnityEngine.Object
		{
			// 1. 确保管理器已完整初始化（Addressables+映射表）
			if (!await InitializeAsync(onError))
				return new List<T>();

			// 2. 校验Label有效性
			if (string.IsNullOrEmpty(label))
			{
				string errorMsg = "加载资源失败：Label不能为空";
				onError?.Invoke(errorMsg);
				ArchLog.Error(errorMsg);
				return new List<T>();
			}

			// 关键修正1：使用IList<IResourceLocation>接收，且移除throwExceptionIfNothingFound参数
			IList<IResourceLocation> resourceLocs = null;
			try
			{
				// 3. 获取该Label下的所有资源定位（修正API参数和返回类型）
				var locsHandle = Addressables.LoadResourceLocationsAsync(
					key: label,
					type: typeof(T) // 过滤指定类型的资源
				);

				await locsHandle;

				if (locsHandle.Status != AsyncOperationStatus.Succeeded)
				{
					string errorMsg = $"获取Label[{label}]的资源定位失败：{locsHandle.OperationException?.Message}";
					onError?.Invoke(errorMsg);
					ArchLog.Error(errorMsg);
					Addressables.Release(locsHandle);
					return new List<T>();
				}

				resourceLocs = locsHandle.Result;
				Addressables.Release(locsHandle);

				// 4. 处理无资源场景
				int totalCount = resourceLocs?.Count ?? 0;
				if (totalCount == 0)
				{
					string tipMsg = $"Label[{label}]下无{typeof(T).Name}类型的资源";
					ArchLog.Debug(tipMsg);
					onLoaded?.Invoke(new List<T>());
					return new List<T>();
				}

				// 5. 初始化加载任务列表与进度跟踪
				var loadTasks = new List<UniTask<T>>();
				var loadedResources = new List<T>();
				int completedCount = 0;

				// 6. 为每个资源定位创建加载任务（修正IResourceLocation类型使用）
				foreach (var loc in resourceLocs)
				{
					if (loc == null || string.IsNullOrEmpty(loc.PrimaryKey)) // 关键修正2：使用IResourceLocation的PrimaryKey
					{
						ArchLog.Warning($"Label[{label}]包含无效资源定位，已跳过");
						completedCount++;
						progressCallback?.Invoke(completedCount, totalCount);
						continue;
					}

					// 6.1 检查是否已有该资源的加载任务
					bool hasExistingTask = false;
					lock (_loadingHandles)
					{
						if (_loadingHandles.TryGetValue(loc.PrimaryKey, out var existingHandle))
						{
							if (!existingHandle.IsDone)
							{
								loadTasks.Add(WaitForExistingLoad<T>(existingHandle, null, null));
								hasExistingTask = true;
							}
							else
							{
								_loadingHandles.Remove(loc.PrimaryKey);
							}
						}
					}

					// 6.2 无现有任务，创建新加载任务
					if (!hasExistingTask)
					{
						loadTasks.Add(LoadSingleResourceByLocAsync<T>(loc, (resource) =>
						{
							lock (_loadingHandles)
							{
								_loadingHandles.Remove(loc.PrimaryKey);
							}

							completedCount++;
							if (resource != null)
								loadedResources.Add(resource);

							progressCallback?.Invoke(completedCount, totalCount);
						}, errorMsg =>
						{
							lock (_loadingHandles)
							{
								_loadingHandles.Remove(loc.PrimaryKey);
							}

							completedCount++;
							progressCallback?.Invoke(completedCount, totalCount);
							ArchLog.Warning($"Label[{label}]下资源[{loc.PrimaryKey}]加载失败：{errorMsg}");
						}));
					}
				}

				// 7. 等待所有加载任务完成
				await UniTask.WhenAll(loadTasks);

				// 8. 最终回调与日志
				string resultMsg = $"Label[{label}]资源加载完成：共{totalCount}个，成功{loadedResources.Count}个，失败{totalCount - loadedResources.Count}个";
				ArchLog.Debug(resultMsg);
				onLoaded?.Invoke(loadedResources);
				return loadedResources;
			}
			catch (Exception ex)
			{
				string errorMsg = $"Label[{label}]资源加载异常：{ex.Message}\n{ex.StackTrace}";
				onError?.Invoke(errorMsg);
				ArchLog.Error(errorMsg);
				return new List<T>();
			}
			finally
			{
				// 清理资源定位引用
				if (resourceLocs != null && resourceLocs is List<IResourceLocation> listLocs)
					listLocs.Clear();
			}
		}

		/// <summary>
		/// 辅助方法：通过IResourceLocation加载单个资源（修正类型）
		/// </summary>
		private static async UniTask<T> LoadSingleResourceByLocAsync<T>(
			IResourceLocation loc, // 关键修正3：参数类型改为IResourceLocation
			Action<T> onSingleLoaded,
			Action<string> onSingleError) where T : UnityEngine.Object
		{
			if (loc == null || string.IsNullOrEmpty(loc.PrimaryKey))
			{
				onSingleError?.Invoke("无效的资源定位");
				return null;
			}

			// 创建加载句柄并记录到字典
			var loadHandle = Addressables.LoadAssetAsync<T>(loc.PrimaryKey);
			lock (_loadingHandles)
			{
				_loadingHandles[loc.PrimaryKey] = loadHandle;
			}

			try
			{
				await loadHandle;

				if (loadHandle.Status == AsyncOperationStatus.Succeeded && loadHandle.Result != null)
				{
					T resource = loadHandle.Result;
					onSingleLoaded?.Invoke(resource);
					ArchLog.Debug($"Label资源加载成功：{loc.PrimaryKey}（类型：{typeof(T).Name}）");
					return resource;
				}
				else
				{
					string errorMsg = loadHandle.OperationException?.Message ?? "资源加载成功但结果为空";
					onSingleError?.Invoke(errorMsg);
					return null;
				}
			}
			catch (Exception ex)
			{
				onSingleError?.Invoke(ex.Message);
				return null;
			}
			finally
			{
				// 若加载失败，释放句柄
				if (loadHandle.IsValid() && (loadHandle.Status == AsyncOperationStatus.Failed || loadHandle.Result == null))
					Addressables.Release(loadHandle);
			}
		}
		#region 资源加载/实例化/释放（保留原有逻辑，优化错误处理）

		/// <summary>
		/// 通过资源名称异步加载资源（集成引用计数，自动复用已加载资源）
		/// </summary>
		public static async UniTask<T> LoadAsync<T>(string resourceName, Action<T> onLoaded = null, Action<string> onError = null) where T : UnityEngine.Object
		{
			// 1. 确保管理器已完整初始化
			if (!await InitializeAsync(onError))
				return null;

			// 2. 从映射表获取Address
			if (!_nameMap.TryGetAddress(resourceName, out string address))
			{
				string errorMsg = $"资源 {resourceName} 不存在于映射表中";
				onError?.Invoke(errorMsg);
				ArchLog.Error(errorMsg);
				return null;
			}

			// 3. 处理重复加载：复用已有资源，引用计数+1

			if (_resourceRecords.TryGetValue(address, out var existingRecord))
			{
				// 引用计数+1
				existingRecord.RefCount++;
				ArchLog.Debug($"复用资源 {resourceName}（Address：{address}），当前引用计数：{existingRecord.RefCount}");

				// 等待已有Handle完成（若仍在加载中）
				if (!existingRecord.Handle.IsDone)
				{

					// 异步等待Handle完成（不阻塞主线程）
					var waitTask = UniTask.Create(async () =>
					{
						while (!existingRecord.Handle.IsDone)
							await UniTask.Yield();
					});
					waitTask.Forget(); // 非阻塞等待
				}

				// 获取结果并回调
				var result = existingRecord.Handle.Result as T;
				onLoaded?.Invoke(result);
				return result;
			}


			// 4. 首次加载：创建新的资源记录
			AsyncOperationHandle<T> loadHandle = default;
			try
			{
				loadHandle = Addressables.LoadAssetAsync<T>(address);

				// 等待加载完成
				var result = await loadHandle;

				if (result != null)
				{
					lock (_recordLock)
					{
						// 双重检查：避免多线程并发创建重复记录
						if (!_resourceRecords.ContainsKey(address))
						{
							// 创建资源记录并加入字典
							var newRecord = new ResourceRecord(address, loadHandle);
							_resourceRecords.Add(address, newRecord);
							ArchLog.Debug($"首次加载资源 {resourceName}（Address：{address}），引用计数：1");
						}
						else
						{
							// 多线程并发场景：复用已有记录，引用计数+1
							_resourceRecords[address].RefCount++;
							ArchLog.Debug($"多线程并发加载资源 {resourceName}，当前引用计数：{_resourceRecords[address].RefCount}");
						}
					}

					onLoaded?.Invoke(result);
					return result;
				}
				else
				{
					string errorMsg = $"资源 {resourceName}（Address：{address}）加载成功但结果为空";
					onError?.Invoke(errorMsg);
					ArchLog.Error(errorMsg);
					return null;
				}
			}
			catch (Exception ex)
			{
				string errorMsg = $"资源 {resourceName}（Address：{address}）加载异常：{ex.Message}";
				onError?.Invoke(errorMsg);
				ArchLog.Error(errorMsg);

				// 加载失败：释放Handle，避免内存泄漏
				if (loadHandle.IsValid())
					Addressables.Release(loadHandle);

				return null;
			}
		}


		/// <summary>
		/// 等待已有加载操作完成（避免重复加载）
		/// </summary>
		private static async UniTask<T> WaitForExistingLoad<T>(AsyncOperationHandle existingHandle, Action<T> onLoaded, Action<string> onError) where T : UnityEngine.Object
		{
			try
			{
				await existingHandle;
				var result = existingHandle.Result as T;

				if (result != null)
				{
					onLoaded?.Invoke(result);
					ArchLog.Debug($"资源加载完成（复用已有操作）：{result.name}");
					return result;
				}
				else
				{
					string errorMsg = $"复用已有加载操作失败：结果为空";
					onError?.Invoke(errorMsg);
					ArchLog.Error(errorMsg);
					return null;
				}
			}
			catch (Exception ex)
			{
				string errorMsg = $"复用已有加载操作异常：{ex.Message}";
				onError?.Invoke(errorMsg);
				ArchLog.Error(errorMsg);
				return null;
			}
		}

		/// <summary>
		/// 通过资源名称实例化预制体（跟踪实例，集成引用计数）
		/// </summary>
		public static async UniTask<GameObject> InstantiateAsync(
			string resourceName,
			Transform parent = null,
			bool worldPositionStays = true,
			Action<GameObject> onInstantiated = null,
			Action<string> onError = null)
		{
			// 1. 先加载预制体（自动触发引用计数）
			var prefab = await LoadAsync<GameObject>(resourceName, onError: onError);
			if (prefab == null)
				return null;

			try
			{
				// 2. 实例化预制体
				var instance = GameObject.Instantiate(prefab, parent, worldPositionStays);
				instance.name = prefab.name; // 移除"(Clone)"后缀

				// 3. 将实例添加到资源记录的实例列表
				lock (_recordLock)
				{
					if (_nameMap.TryGetAddress(resourceName, out string address) && _resourceRecords.TryGetValue(address, out var record))
					{
						record.AddInstance(instance);
					}
					else
					{
						ArchLog.Warning($"实例化资源 {resourceName} 未找到对应记录，无法跟踪实例（可能已被释放）");
					}
				}

				onInstantiated?.Invoke(instance);
				return instance;
			}
			catch (Exception ex)
			{
				string errorMsg = $"预制体 {resourceName} 实例化异常：{ex.Message}";
				onError?.Invoke(errorMsg);
				ArchLog.Error(errorMsg);
				return null;
			}
		}


		/// <summary>
		/// 释放资源（基于引用计数，计数为0时彻底释放底层资源）
		/// </summary>
		public static void Release<T>(string resourceName, T instance = null, bool releaseAsset = false) where T : UnityEngine.Object
		{
			// 1. 处理实例释放（若传入实例）
			if (instance != null)
			{
				bool isInstanceRemoved = false;
				string targetAddress = null;

				lock (_recordLock)
				{
					// 找到实例对应的资源记录
					if (_nameMap.TryGetAddress(resourceName, out targetAddress) && _resourceRecords.TryGetValue(targetAddress, out var record))
					{
						// 从记录中移除实例
						isInstanceRemoved = record.RemoveInstance(instance);
						if (isInstanceRemoved)
						{
							ArchLog.Debug($"从资源 {resourceName}（Address：{targetAddress}）移除实例，剩余实例数：{record.InstanceObjects.Count}");
						}
					}
				}

				// 销毁实例（无论是否找到记录，避免内存泄漏）
				if (isInstanceRemoved || !_isNameMapInited)
				{
					if (instance is GameObject go)
					{
#if UNITY_EDITOR
						if (!EditorApplication.isPlaying)
							GameObject.DestroyImmediate(go);
						else
#endif
							GameObject.Destroy(go);
					}
					else
					{
#if UNITY_EDITOR
						if (!EditorApplication.isPlaying)
							UnityEngine.Object.DestroyImmediate(instance);
						else
#endif
							UnityEngine.Object.Destroy(instance);
					}
				}
				else
				{
					ArchLog.Warning($"释放实例 {instance.name} 失败：未找到对应资源记录，可能已被释放");
					return;
				}
			}

			// 2. 处理底层资源释放（引用计数为0或强制释放时）
			if (!_nameMap.TryGetAddress(resourceName, out string address))
			{
				ArchLog.Warning($"释放底层资源失败：{resourceName} 不存在于映射表中");
				return;
			}

			lock (_recordLock)
			{
				if (!_resourceRecords.TryGetValue(address, out var record))
				{
					ArchLog.Warning($"释放底层资源失败：{resourceName}（Address：{address}）无资源记录，可能已释放");
					return;
				}

				// 引用计数-1
				record.RefCount--;

				// 条件：引用计数≤0 或 强制释放（releaseAsset=true）
				if (record.RefCount <= 0 || releaseAsset)
				{
					// 步骤1：销毁所有剩余实例
					record.DestroyAllInstances();

					// 步骤2：释放底层资源Handle
					record.ReleaseHandle();

					// 步骤3：从字典中移除记录
					_resourceRecords.Remove(address);
					ArchLog.Debug($"资源 {resourceName}（Address：{address}）已彻底释放（引用计数≤0或强制释放）");
				}
			}
		}
		#region 新增：强制释放所有资源
		/// <summary>
		/// 强制释放所有已加载资源：
		/// 1. 销毁所有实例化Object（无论是否在使用）
		/// 2. 释放所有资源Handle
		/// 3. 清空资源记录字典
		/// </summary>
		public static void ForceRelease()
		{
			lock (_recordLock)
			{
				if (_resourceRecords.Count == 0)
				{
					ArchLog.Debug("ForceRelease：无已加载资源，无需释放");
					return;
				}

				// 遍历所有资源记录，强制销毁并释放
				foreach (var (address, record) in _resourceRecords)
				{
					try
					{
						// 1. 强制销毁所有实例
						record.DestroyAllInstances();

						// 2. 强制释放Handle
						record.ReleaseHandle();

						ArchLog.Debug($"ForceRelease：资源 {address} 强制释放完成");
					}
					catch (Exception ex)
					{
						ArchLog.Error($"ForceRelease：处理资源 {address} 异常：{ex.Message}\n{ex.StackTrace}");
					}
				}

				// 3. 清空所有资源记录
				_resourceRecords.Clear();
				ArchLog.Debug($"ForceRelease：所有资源记录已清空，共释放 {_resourceRecords.Count} 个资源");
			}

			// 额外清理：重置初始化状态（可选，根据业务需求决定是否保留）
			// _isAddressablesInited = false;
			// _isNameMapInited = false;
			// _nameMap = null;

			ArchLog.Debug("ForceRelease：强制释放流程完成");
		}
		#endregion


		#endregion
		#region 新增：资源引用计数与实例跟踪核心结构
		/// <summary>
		/// 资源记录：管理单个资源的引用计数、Handle、实例化对象
		/// </summary>
		private class ResourceRecord
		{
			/// <summary>
			/// 引用计数（加载一次+1，释放一次-1，为0时可销毁资源）
			/// </summary>
			public int RefCount { get; set; }

			/// <summary>
			/// 资源的Addressables Handle
			/// </summary>
			public AsyncOperationHandle Handle { get; private set; }

			/// <summary>
			/// 该资源实例化出的所有Object（如预制体实例）
			/// </summary>
			public List<UnityEngine.Object> InstanceObjects { get; private set; }

			/// <summary>
			/// 资源对应的Address（用于索引）
			/// </summary>
			public string Address { get; private set; }

			public ResourceRecord(string address, AsyncOperationHandle handle)
			{
				Address = address;
				Handle = handle;
				RefCount = 1; // 初始加载时引用计数为1
				InstanceObjects = new List<UnityEngine.Object>();
			}

			/// <summary>
			/// 添加实例化对象到记录
			/// </summary>
			public void AddInstance(UnityEngine.Object instance)
			{
				if (instance != null && !InstanceObjects.Contains(instance))
				{
					InstanceObjects.Add(instance);
				}
			}

			/// <summary>
			/// 从记录中移除实例化对象
			/// </summary>
			public bool RemoveInstance(UnityEngine.Object instance)
			{
				return InstanceObjects.Remove(instance);
			}

			/// <summary>
			/// 销毁所有实例化对象
			/// </summary>
			public void DestroyAllInstances()
			{
				foreach (var obj in InstanceObjects)
				{
					if (obj == null) continue;

					// 区分GameObject和其他资源（适配运行时/Editor）
					if (obj is GameObject go)
					{
#if UNITY_EDITOR
						if (!EditorApplication.isPlaying)
							GameObject.DestroyImmediate(go); // Editor非运行时用立即销毁
						else
#endif
							GameObject.Destroy(go); // 运行时用正常销毁（走生命周期）
					}
					else
					{
#if UNITY_EDITOR
						if (!EditorApplication.isPlaying)
							UnityEngine.Object.DestroyImmediate(obj);
						else
#endif
							UnityEngine.Object.Destroy(obj);
					}

					ArchLog.Debug($"销毁实例：{obj.name}（资源Address：{Address}）");
				}
				InstanceObjects.Clear();
			}

			/// <summary>
			/// 释放Handle（仅当Handle有效时）
			/// </summary>
			public void ReleaseHandle()
			{
				if (Handle.IsValid())
				{
					Addressables.Release(Handle);
					ArchLog.Debug($"释放资源Handle：{Address}");
				}
			}
		}

		/// <summary>
		/// 资源记录字典：Key=Address，Value=资源记录（线程安全）
		/// </summary>
		private static readonly Dictionary<string, ResourceRecord> _resourceRecords = new Dictionary<string, ResourceRecord>();

		/// <summary>
		/// 锁对象：保护_resourceRecords的线程安全访问
		/// </summary>
		private static readonly object _recordLock = new object();

		// 移除原有冗余的_loadingHandles（用_resourceRecords替代）
		// private static readonly Dictionary<string, AsyncOperationHandle> _loadingHandles = new Dictionary<string, AsyncOperationHandle>();
		#endregion

	}
}
