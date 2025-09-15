using Arch.Core;

namespace Arch
{
    #region Component

    public interface IComponent
    {
    }

    public interface IModelComponent : IComponent
    {
    }

    public interface IViewComponent : IComponent
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

    public interface IReactiveAwake : IReactiveSystem
    {
        public void SubcribeEntityAwake();
    }

    public interface IReactiveUpdate : IReactiveSystem
    {
        public void Update();
    }

    public interface IReactiveLateUpdate : IReactiveSystem
    {
        public void LateUpdate();
    }

    public interface IReactiveDestroy : IReactiveSystem
    {
        public void SubcribeEntityDestroy();
    }

    public interface IAwake : ISystem
    {
        public void Awake();
    }

    public interface IUpdate : ISystem
    {
        public void Update();
    }

    public interface ILateUpdate : ISystem
    {
        public void LateUpdate();
    }

    public interface IDestroy : ISystem
    {
        public void Destroy();
    }

    #endregion System
}