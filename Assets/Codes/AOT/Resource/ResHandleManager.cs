
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


namespace Arch
{

	// 资源句柄数据类
	public class ResHandleData
	{
		public string Key;
		public AsyncOperationHandle Handle;
		public int RefCount;
		public List<UnityEngine.Object> Instances = new List<UnityEngine.Object>();
	}

	/// <summary>
	/// 资源句柄管理器
	/// </summary>
	public class ResHandleManager
	{
		private static ResHandleManager _instance;
		public static ResHandleManager Instance => _instance ??= new ResHandleManager();

		// 存储资源句柄和对应的实例化对象
		private Dictionary<string, ResHandleData> _handleDict = new Dictionary<string, ResHandleData>();

		/// <summary>
		/// 添加资源句柄
		/// </summary>
		public void AddHandle(string key, AsyncOperationHandle handle, UnityEngine.Object instance = null)
		{
			if (_handleDict.TryGetValue(key, out var data))
			{
				data.RefCount++;
				if (instance != null && !data.Instances.Contains(instance))
				{
					data.Instances.Add(instance);
				}
			}
			else
			{
				data = new ResHandleData
				{
					Handle = handle,
					RefCount = 1,
					Key = key
				};

				if (instance != null)
				{
					data.Instances.Add(instance);
				}

				_handleDict[key] = data;
			}
		}

		/// <summary>
		/// 释放资源
		/// </summary>
		public void Release(string key, UnityEngine.Object instance = null)
		{
			if (_handleDict.TryGetValue(key, out var data))
			{
				if (instance != null && data.Instances.Contains(instance))
				{
					data.Instances.Remove(instance);

					// 销毁实例
					if (instance is GameObject gameObj)
					{
						UnityEngine.Object.Destroy(gameObj);
					}
					else if (instance is Component component)
					{
						UnityEngine.Object.Destroy(component.gameObject);
					}
				}

				data.RefCount--;

				if (data.RefCount <= 0)
				{
					Addressables.Release(data.Handle);
					_handleDict.Remove(key);
				}
			}
		}

		/// <summary>
		/// 强制销毁资源
		/// </summary>
		public void ForceRelease(string key)
		{
			if (_handleDict.TryGetValue(key, out var data))
			{
				// 先销毁所有实例
				foreach (var instance in data.Instances)
				{
					if (instance is GameObject gameObj)
					{
						UnityEngine.Object.Destroy(gameObj);
					}
					else if (instance is Component component)
					{
						UnityEngine.Object.Destroy(component.gameObject);
					}
				}
				data.Instances.Clear();

				// 释放资源句柄
				Addressables.Release(data.Handle);
				_handleDict.Remove(key);
			}
		}

		/// <summary>
		/// 获取资源引用计数
		/// </summary>
		public int GetRefCount(string key)
		{
			return _handleDict.TryGetValue(key, out var data) ? data.RefCount : 0;
		}
	}
}

