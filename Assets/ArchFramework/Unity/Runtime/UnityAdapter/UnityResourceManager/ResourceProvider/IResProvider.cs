using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace Arch.Resource
{
	public interface IResProvider
	{
		UniTask<T> LoadAsync<T>(string name) where T : class;

		UniTask InitializeAsync();

		UniTask<IEnumerable<T>> LoadAllByLabelAsync<T>(string label) where T : class;

		void Release(string name);

		void ReleaseAll();

		IEnumerable<T> LoadAllByLabel<T>(string aotLabel) where T : class;
	}
}