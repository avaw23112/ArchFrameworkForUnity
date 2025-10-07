#if UNITY_2020_1_OR_NEWER

using Arch.Tools;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Arch.Resource
{
	public class UnityResProvider : IResProvider
	{
		private bool _initialized;

		// 全局：短名 -> 地址（去路径/去扩展名）
		private readonly Dictionary<string, string> _name2Addr = new(StringComparer.OrdinalIgnoreCase);

		// 按标签缓存的二级映射：label -> (短名 -> 地址)
		private readonly Dictionary<string, Dictionary<string, string>> _labelName2Addr = new(StringComparer.OrdinalIgnoreCase);

		// 地址 -> 句柄/计数
		private readonly Dictionary<string, AsyncOperationHandle> _cache = new();

		private readonly Dictionary<string, int> _refCount = new();

		// 实例 -> 地址（实例参与引用计数）
		private readonly Dictionary<UnityEngine.Object, string> _instanceAddr = new();

		// 重名记录（仅告警）
		private readonly Dictionary<string, List<string>> _duplicates = new(StringComparer.OrdinalIgnoreCase);

		#region 初始化

		public void RefreshMappings()
		{
			_initialized = false;
			InitializeAsync().Forget();
		}

		public async UniTask InitializeAsync()
		{
			if (_initialized)
				return;

			await Addressables.InitializeAsync().Task;

			// 🚀 预分配容量
			var visitedAddrs = new HashSet<string>(1024, StringComparer.OrdinalIgnoreCase);
			_name2Addr.Clear();
			_duplicates.Clear();

			// 🚀 并行处理每个 locator
			var tasks = new List<UniTask>();

			foreach (var locator in Addressables.ResourceLocators)
			{
				if (locator == null)
					continue;

#if UNITY_EDITOR
				if (locator.GetType().Name == "AddressableAssetSettingsLocator")
					continue; // 跳过编辑器定位器
#endif

				if (locator.GetType().Name != "ResourceLocationMap")
					continue; // 🚀 仅扫描主映射表（性能最佳）

				if (locator.Keys == null)
					continue;

				tasks.Add(ProcessLocatorAsync(locator, visitedAddrs));
			}

			await UniTask.WhenAll(tasks);

			_initialized = true;
			ArchLog.LogInfo($"[Res] Initialized. Entries={_name2Addr.Count}, Duplicates={_duplicates.Count}");
		}

		private async UniTask ProcessLocatorAsync(IResourceLocator locator, HashSet<string> visited)
		{
			// 🚀 非阻塞批量处理 Keys
			await UniTask.SwitchToThreadPool();

			// 局部缓存以减少锁争用
			var localMap = new Dictionary<string, string>(256, StringComparer.OrdinalIgnoreCase);
			var localDup = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

			foreach (var keyObj in locator.Keys)
			{
				if (keyObj is not string addr || string.IsNullOrWhiteSpace(addr))
					continue;

				if (!visited.Add(addr))
					continue;

				// 🔒 TryLocate 代替直接 Locate（更安全）
				if (!locator.Locate(addr, typeof(UnityEngine.Object), out var locations) || locations == null)
					continue;

				for (int i = 0; i < locations.Count; i++)
				{
					var loc = locations[i];
					if (loc == null)
						continue;

					string shortName = ShortNameFast(loc);
					if (string.IsNullOrEmpty(shortName))
						continue;

					if (localMap.ContainsKey(shortName))
					{
						if (!localDup.TryGetValue(shortName, out var list))
							localDup[shortName] = list = new List<string> { localMap[shortName] };
						list.Add(addr);
						continue;
					}

					localMap[shortName] = addr;
				}
			}

			// 🚀 主线程合并（避免线程冲突）
			await UniTask.SwitchToMainThread();

			foreach (var kv in localMap)
			{
				if (_name2Addr.ContainsKey(kv.Key))
				{
					if (!_duplicates.TryGetValue(kv.Key, out var list))
						_duplicates[kv.Key] = list = new List<string> { _name2Addr[kv.Key] };
					list.Add(kv.Value);
				}
				else
				{
					_name2Addr[kv.Key] = kv.Value;
				}
			}

			foreach (var kv in localDup)
			{
				if (!_duplicates.TryGetValue(kv.Key, out var list))
					_duplicates[kv.Key] = kv.Value;
				else
					list.AddRange(kv.Value);
			}
		}

		#endregion 初始化

		#region 公共加载

		// 路径无关（短名）
		public async UniTask<T> LoadAsync<T>(string name) where T : class
		{
			if (!_initialized) await InitializeAsync();

			if (!_name2Addr.TryGetValue(name, out var addr))
			{
				ArchLog.LogError($"[Res] Resource not found: {name}");
				return null;
			}
			return await LoadByAddressAsync<T>(addr);
		}

		// 标签 + 名称 分层加载
		public async UniTask<T> LoadAsync<T>(string label, string name) where T : class
		{
			if (!_initialized) await InitializeAsync();

			string addr = await ResolveAddressByLabelAndName(label, name);
			if (addr == null)
			{
				ArchLog.LogError($"[Res] Not found: label='{label}', name='{name}'");
				return null;
			}
			return await LoadByAddressAsync<T>(addr);
		}

		// 标签批量加载（每个结果都有独立句柄与计数）
		public async UniTask<IEnumerable<T>> LoadAllByLabelAsync<T>(string label) where T : class
		{
			if (!_initialized) await InitializeAsync();

			var locHandle = Addressables.LoadResourceLocationsAsync(label);
			var locs = await locHandle.Task;
			if (locs == null)
			{
				ArchLog.LogError($"[Res] LoadAllByLabel: no locations for label '{label}'");
				return null;
			}

			var list = new List<T>();
			foreach (var loc in locs)
			{
				string addr = loc.PrimaryKey;
				var obj = await LoadByAddressAsync<T>(addr);
				if (obj != null) list.Add(obj);
			}

			Addressables.Release(locHandle);
			return list;
		}

		public IEnumerable<T> LoadAllByLabel<T>(string label) where T : class
		{
			// 为简洁起见：同步方式直接 WaitForCompletion，每项单独加载（句柄独立、计数独立）
			var locHandle = Addressables.LoadResourceLocationsAsync(label);
			var locs = locHandle.WaitForCompletion();
			var list = new List<T>();
			if (locs != null)
			{
				foreach (var loc in locs)
				{
					string addr = loc.PrimaryKey;
					if (_cache.TryGetValue(addr, out var h))
					{
						AddRef(addr);
						list.Add(h.Result as T);
					}
					else
					{
						var h2 = Addressables.LoadAssetAsync<UnityEngine.Object>(addr);
						var r = h2.WaitForCompletion();
						if (r != null)
						{
							_cache[addr] = h2;
							_refCount[addr] = 1;
							list.Add(r as T);
						}
					}
				}
			}
			Addressables.Release(locHandle);
			return list;
		}

		#endregion 公共加载

		#region 实例化 / 销毁（参与计数）

		public async UniTask<GameObject> InstantiateAsync(string name, Transform parent = null)
		{
			var prefab = await LoadAsync<GameObject>(name);
			if (prefab == null) return null;
			var inst = UnityEngine.Object.Instantiate(prefab, parent);
			_instanceAddr[inst] = _name2Addr[name];
			AddRef(_name2Addr[name]);
			return inst;
		}

		public async UniTask<GameObject> InstantiateAsync(string label, string name, Transform parent = null)
		{
			var prefab = await LoadAsync<GameObject>(label, name);
			if (prefab == null) return null;

			string addr = await ResolveAddressByLabelAndName(label, name);
			var inst = UnityEngine.Object.Instantiate(prefab, parent);
			_instanceAddr[inst] = addr;
			AddRef(addr);
			return inst;
		}

		// 建议：通过此接口销毁实例，保证计数正确回收
		public void DestroyInstance(UnityEngine.Object instance)
		{
			if (instance == null) return;
			if (_instanceAddr.TryGetValue(instance, out var addr))
			{
				_instanceAddr.Remove(instance);
				ReleaseRef(addr);
			}
			if (instance is GameObject go) UnityEngine.Object.Destroy(go);
			else UnityEngine.Object.Destroy(instance);
		}

		#endregion 实例化 / 销毁（参与计数）

		#region 卸载（安全/强制）

		public void Release(string name)
		{
			if (!_name2Addr.TryGetValue(name, out var addr)) return;
			ReleaseInternal(addr, name, strict: true);
		}

		public void Release(string label, string name)
		{
			var addr = ResolveAddressByLabelAndName(label, name).GetAwaiter().GetResult();
			if (addr == null) return;
			ReleaseInternal(addr, $"{label}/{name}", strict: true);
		}

		public void ForceRelease(string name)
		{
			if (!_name2Addr.TryGetValue(name, out var addr)) return;
			ForceReleaseInternal(addr, name);
		}

		public void ForceRelease(string label, string name)
		{
			var addr = ResolveAddressByLabelAndName(label, name).GetAwaiter().GetResult();
			if (addr == null) return;
			ForceReleaseInternal(addr, $"{label}/{name}");
		}

		public void ForceReleaseLabel(string label)
		{
			if (!_labelName2Addr.TryGetValue(label, out var map)) return;
			foreach (var kv in map)
				ForceReleaseInternal(kv.Value, $"{label}/{kv.Key}");
		}

		public void ReleaseAll()
		{
			foreach (var kv in _cache)
				Addressables.Release(kv.Value);
			_cache.Clear();
			_refCount.Clear();
			_instanceAddr.Clear();
			Resources.UnloadUnusedAssets();
			ArchLog.LogInfo("[Res] All released.");
		}

		#endregion 卸载（安全/强制）

		#region 内部：加载/计数/映射

		private async UniTask<T> LoadByAddressAsync<T>(string addr) where T : class
		{
			if (_cache.TryGetValue(addr, out var h))
			{
				AddRef(addr);
				return h.Result as T;
			}

			var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(addr);
			await handle.Task;
			if (handle.Status != AsyncOperationStatus.Succeeded)
			{
				ArchLog.LogError($"[Res] Load failed: {addr}");
				return null;
			}

			_cache[addr] = handle;
			_refCount[addr] = 1;
			return handle.Result as T;
		}

		private async UniTask<string> ResolveAddressByLabelAndName(string label, string name)
		{
			// 命中缓存
			if (_labelName2Addr.TryGetValue(label, out var map) && map.TryGetValue(name, out var cached))
				return cached;

			// 现查 locations（注意：IResourceLocation 没有 Labels，只能用 label 去查）
			var locHandle = Addressables.LoadResourceLocationsAsync(label);
			var locs = await locHandle.Task;
			if (locs == null) { Addressables.Release(locHandle); return null; }

			map ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var loc in locs)
			{
				string shortName = ShortNameFast(loc);
				string addr = loc.PrimaryKey;
				if (!map.ContainsKey(shortName)) map[shortName] = addr;
				else ArchLog.LogWarning($"[Res] Duplicate '{shortName}' under label '{label}'.");
			}
			Addressables.Release(locHandle);

			_labelName2Addr[label] = map;
			map.TryGetValue(name, out var result);
			return result;
		}

		private static string ShortNameFast(IResourceLocation loc)
		{
			// 优先 PrimaryKey（一般是 Address 或 Asset GUID）
			var key = loc?.PrimaryKey;
			if (!string.IsNullOrEmpty(key))
			{
				var span = key.AsSpan();

				// 判断是否有扩展名（快速判断）
				int dot = span.LastIndexOf('.');
				if (dot > 0 && dot < span.Length - 1)
				{
					// 获取最后的 '/' 或 '\'
					int slash = Math.Max(span.LastIndexOf('/'), span.LastIndexOf('\\'));
					ReadOnlySpan<char> nameSpan = (slash >= 0)
						? span[(slash + 1)..dot]
						: span[..dot];

					if (!nameSpan.IsEmpty)
						return new string(nameSpan);
				}
			}

			// 次选 InternalId（例如 Assets/... 或 file:///）
			var id = loc?.InternalId;
			if (!string.IsNullOrEmpty(id))
			{
				var span = id.AsSpan();
				int dot = span.LastIndexOf('.');
				if (dot > 0 && dot < span.Length - 1)
				{
					int slash = Math.Max(span.LastIndexOf('/'), span.LastIndexOf('\\'));
					ReadOnlySpan<char> nameSpan = (slash >= 0)
						? span[(slash + 1)..dot]
						: span[..dot];

					if (!nameSpan.IsEmpty)
						return new string(nameSpan);
				}
			}

			// 没有扩展名，说明是文件夹或非法项
			return string.Empty;
		}

		private void AddRef(string addr)
		{
			if (string.IsNullOrEmpty(addr)) return;
			_refCount.TryGetValue(addr, out var c);
			_refCount[addr] = c + 1;
		}

		private void ReleaseRef(string addr)
		{
			if (string.IsNullOrEmpty(addr)) return;
			if (_refCount.TryGetValue(addr, out var c))
			{
				c--;
				if (c <= 0) _refCount.Remove(addr);
				else _refCount[addr] = c;
			}
		}

		private void ReleaseInternal(string addr, string displayName, bool strict)
		{
			if (!_cache.TryGetValue(addr, out var handle)) return;

			if (strict && _refCount.TryGetValue(addr, out int c) && c > 0)
			{
				ArchLog.LogWarning($"[Res] Cannot release '{displayName}', refCount={c}");
				return;
			}

			Addressables.Release(handle);
			_cache.Remove(addr);
			_refCount.Remove(addr);
			ArchLog.LogInfo($"[Res] Released: {displayName}");
		}

		private void ForceReleaseInternal(string addr, string displayName)
		{
			// 先销毁所有实例（它们会持有计数）
			var toDestroy = new List<UnityEngine.Object>();
			foreach (var kv in _instanceAddr)
				if (kv.Value == addr) toDestroy.Add(kv.Key);
			foreach (var inst in toDestroy)
			{
				_instanceAddr.Remove(inst);
				if (inst is GameObject go) UnityEngine.Object.Destroy(go);
				else UnityEngine.Object.Destroy(inst);
			}

			// 释放句柄与计数
			if (_cache.TryGetValue(addr, out var handle))
			{
				Addressables.Release(handle);
				_cache.Remove(addr);
			}
			_refCount.Remove(addr);
			ArchLog.LogInfo($"[Res] Force released: {displayName}");
		}

		#endregion 内部：加载/计数/映射
	}
}

#endif