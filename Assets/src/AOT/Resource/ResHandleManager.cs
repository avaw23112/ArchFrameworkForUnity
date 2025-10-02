
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


namespace Arch
{

	// ��Դ���������
	public class ResHandleData
	{
		public string Key;
		public AsyncOperationHandle Handle;
		public int RefCount;
		public List<UnityEngine.Object> Instances = new List<UnityEngine.Object>();
	}

	/// <summary>
	/// ��Դ���������
	/// </summary>
	public class ResHandleManager
	{
		private static ResHandleManager _instance;
		public static ResHandleManager Instance => _instance ??= new ResHandleManager();

		// �洢��Դ����Ͷ�Ӧ��ʵ��������
		private Dictionary<string, ResHandleData> _handleDict = new Dictionary<string, ResHandleData>();

		/// <summary>
		/// �����Դ���
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
		/// �ͷ���Դ
		/// </summary>
		public void Release(string key, UnityEngine.Object instance = null)
		{
			if (_handleDict.TryGetValue(key, out var data))
			{
				if (instance != null && data.Instances.Contains(instance))
				{
					data.Instances.Remove(instance);

					// ����ʵ��
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
		/// ǿ��������Դ
		/// </summary>
		public void ForceRelease(string key)
		{
			if (_handleDict.TryGetValue(key, out var data))
			{
				// ����������ʵ��
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

				// �ͷ���Դ���
				Addressables.Release(data.Handle);
				_handleDict.Remove(key);
			}
		}

		/// <summary>
		/// ��ȡ��Դ���ü���
		/// </summary>
		public int GetRefCount(string key)
		{
			return _handleDict.TryGetValue(key, out var data) ? data.RefCount : 0;
		}
	}
}

