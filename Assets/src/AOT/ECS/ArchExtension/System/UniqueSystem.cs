using Arch.Core;

namespace Arch
{
	public abstract class UniqueComponentSystem<T> : DestroySystem<T>, IAwake
		where T : IComponent
	{
		public void Awake()
		{
			SingletonComponent.Setter((ref T component) => OnAwake(ref component));
		}
		protected override void Run(Entity entity, ref T component_T1)
		{
			OnDestroy(ref component_T1);
		}
		protected abstract void OnAwake(ref T component);
		protected abstract void OnDestroy(ref T component);
	}
}
