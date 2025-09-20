using Cysharp.Threading.Tasks;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Assets.Scripts.Test.AddressabelsTest
{
	public class AddressableTest_2_Reference : MonoBehaviour
	{
		public AssetReference m_assetReference;
		// Use this for initialization
		async void Start()
		{
			GameObject pObject = await m_assetReference.LoadAssetAsync<GameObject>()
			.WithCancellation(this.GetCancellationTokenOnDestroy());
			GameObject.Instantiate(pObject);
		}

		// Update is called once per frame
		void Update()
		{

		}
	}
}