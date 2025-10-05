using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace Arch.Compilation.Editor
{
	[GlobalPostBuildProcessor]
	internal class AutoBuildBundleProcessor : IGlobalPostProcessor
	{
		public string Name => "Addressables自动构建流程";
		public string Description => "用于自动调用Addressables的打包流程";

		public void Process(ArchBuildConfig cfg)
		{
			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
			AddressableAssetSettings.BuildPlayerContent();
		}
	}
}