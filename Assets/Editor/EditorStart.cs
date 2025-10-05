using UnityEditor;

[InitializeOnLoad]
public class EditorStart
{
	static EditorStart()
	{
		CsprojPatcher
			.Begin()
			.Target("Protocol")
			.AddSourceFolder(@"..\Common\Protocol")
			.Apply();
	}
}