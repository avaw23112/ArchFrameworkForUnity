using System.Collections.Generic;

namespace Arch.Compilation.Editor
{
	public interface ITargetRegistry
	{
		IEnumerable<object> All();

		bool TryGet(string name, out object processor);

		void RegisterAll();
	}
}