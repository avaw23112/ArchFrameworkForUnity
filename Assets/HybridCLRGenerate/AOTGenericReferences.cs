using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"AOT.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Events.Event<Assets.Scripts.GameStartEvent>
	// System.Action<Assets.Scripts.GameStartEvent>
	// }}

	public void RefMethods()
	{
	}
}