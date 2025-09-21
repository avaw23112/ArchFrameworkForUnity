using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Assets.Scripts.Test.AddressabelsTest
{
	public class AddressableTest_4_Label : MonoBehaviour
	{
		public AssetLabelReference m_AssetLabel;
		// Use this for initialization
		void Start()
		{
			Addressables.LoadAssetsAsync<GameObject>(m_AssetLabel, (texture) =>
			{
				Debug.Log("加载了一个资源： " + texture.name);
			});
		}

		// Update is called once per frame
		void Update()
		{

		}
	}
}