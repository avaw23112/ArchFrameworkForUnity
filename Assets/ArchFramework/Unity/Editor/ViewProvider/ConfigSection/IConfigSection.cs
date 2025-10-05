using UnityEditor;

namespace Arch.Compilation.Editor
{
	public interface IConfigSection
	{
		string SectionName { get; }

		void OnGUI(SerializedObject so);
	}
}