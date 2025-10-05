using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Arch.Tools
{
	public interface IAssemblyLoader
	{
		Task<IEnumerable<Assembly>> LoadAssembliesAsync();

		IEnumerable<Assembly> LoadAssemblies();

		void RegisterAssembly(Assembly assembly);

		IEnumerable<Assembly> GetAllAssemblies();
	}
}