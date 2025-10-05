using System;
using Arch.Core;
using Arch.Core.Extensions;

namespace Arch.Net
{
    public sealed class UnitBuilder
    {
        private readonly World _world;
        private Entity _entity;
        private bool _hasEntity;
        private bool _markNetworked;
        private string _name;
        private Action<Entity> _configure;

        private UnitBuilder(World world) { _world = world; }
        private UnitBuilder(Entity entity) { _world = Arch.NamedWorld.DefaultWord; _entity = entity; _hasEntity = true; }

        public static UnitBuilder Begin(World world) => new UnitBuilder(world);
        public static UnitBuilder From(Entity entity) => new UnitBuilder(entity);

        /// <summary>
        /// Mark as networked Unit (auto add NetworkOwner/NetworkEntityId/Unit at Build)
        /// </summary>
        public UnitBuilder Networked() { _markNetworked = true; return this; }


        public UnitBuilder WithName(string name) { _name = name; return this; }


        public UnitBuilder With<T>(in T component) where T : struct, IComponent
        {
            EnsureEntity();
            if (_entity.Has<T>()) _entity.Set(in component);
            else { _entity.Add<T>(); _entity.Set(in component); }
            return this;
        }


        public UnitBuilder Configure(Action<Entity> configure)
        {
            _configure += configure; return this;
        }


        public Entity Build()
        {
            EnsureEntity();
            UnitFactory.EnsureAsUnit(ref _entity, networked: _markNetworked); if (!string.IsNullOrEmpty(_name)) { if (!_entity.Has<UnitName>()) _entity.Add<UnitName>(); _entity.Setter((ref UnitName n) => n.Value = _name); }
            _configure?.Invoke(_entity);
            return _entity;
        }

        private void EnsureEntity()
        {
            if (_hasEntity) return;
            _entity = _world.Create();
            _hasEntity = true;
        }
    }
}





