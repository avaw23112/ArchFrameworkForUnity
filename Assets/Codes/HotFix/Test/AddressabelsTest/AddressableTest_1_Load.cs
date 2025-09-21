using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableTest_1_Load : MonoBehaviour
{
	async void Start()
	{
		Load();
	}

	async void LoadAsync()
	{
		GameObject gameObject = await Addressables.LoadAssetAsync<GameObject>("bb");
		GameObject.Instantiate(gameObject);
	}
	void Load()
	{
		var loadTask = Addressables.LoadAsset<GameObject>("bb");
		if (loadTask.Status == AsyncOperationStatus.Succeeded)
		{
			GameObject gameObject = loadTask.Result;
			GameObject.Instantiate(gameObject);
		}
		else
		{
			Debug.LogError("Failed to load asset with key 'bb'");
		}
	}
}
