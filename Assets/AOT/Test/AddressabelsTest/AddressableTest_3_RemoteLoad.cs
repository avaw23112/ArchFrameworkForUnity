using Arch.Tools;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Assets.Scripts.Test.AddressabelsTest
{
	public class AddressableTest_3_RemoteLoad : MonoBehaviour
	{
		async void Start()
		{
			try
			{
				// 等待缓存系统准备就绪
				await WaitForCacheReady();

				// 现在尝试加载资源
				GameObject o = await Addressables.LoadAssetAsync<GameObject>("dd");
				if (o != null)
				{
					GameObject.Instantiate(o);
					ArchLog.LogInfo("Resource loaded and instantiated successfully.");
				}
				else
				{
					ArchLog.LogError("Failed to load resource: loaded object is null.");
				}
			}
			catch (Exception e)
			{
				ArchLog.LogError($"Exception: {e.Message}");
				ArchLog.LogError($"Stack Trace: {e.StackTrace}");
			}
		}

		private async System.Threading.Tasks.Task WaitForCacheReady()
		{
			int maxWaitTime = 5000; // 最大等待时间（毫秒）
			int elapsedTime = 0;

			while (!Caching.ready && elapsedTime < maxWaitTime)
			{
				await System.Threading.Tasks.Task.Delay(100);
				elapsedTime += 100;
			}

			if (!Caching.ready)
			{
				throw new Exception("Cache system not ready after waiting.");
			}
		}

	}
}