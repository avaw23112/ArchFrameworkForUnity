using Arch.Core;

namespace Arch
{
	#region Component

	public interface IComponent
	{
	}

	public interface ITag : IComponent
	{
	}

	#endregion Component

	#region System

	public interface ISystem
	{
	}

	public interface IGlobalSystem
	{
	}

	public interface IReactiveSystem
	{
		public void BuildIn(World world);

		public QueryDescription Filter();

		public bool GetTrigger(Entity entity);
	}

	public interface IUpdate
	{
		public void Update();
	}

	public interface ILateUpdate
	{
		public void LateUpdate();
	}

	public interface IReactiveAwake : IReactiveSystem
	{
		public void SubcribeEntityAwake();
	}

	public interface IReactiveUpdate : IReactiveSystem, IUpdate
	{
	}

	public interface IReactiveLateUpdate : IReactiveSystem, ILateUpdate
	{
	}

	public interface IReactiveDestroy : IReactiveSystem
	{
		public void SubcribeEntityDestroy();
	}

	public interface IPureAwake : ISystem
	{
		public void Awake();
	}

	public interface IPureUpdate : ISystem, IUpdate
	{
	}

	public interface IPureLateUpdate : ISystem, ILateUpdate
	{
	}

	public interface IPureDestroy : ISystem
	{
		public void Destroy();
	}

	#endregion System
}