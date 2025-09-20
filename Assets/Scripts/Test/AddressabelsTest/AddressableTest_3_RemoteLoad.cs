using Cysharp.Threading.Tasks;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Assets.Scripts.Test.AddressabelsTest
{
	public class AddressableTest_3_RemoteLoad : MonoBehaviour
	{
		async void Start()
		{
			GameObject gameObject = await Addressables.LoadAssetAsync<GameObject>("bb");
			GameObject.Instantiate(gameObject);
		}

		// Update is called once per frame
		void Update()
		{

		}
	}
}