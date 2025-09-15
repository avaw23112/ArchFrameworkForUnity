using Arch.Buffer;
using Arch.Core;
using Arch.Tools.Pool;

namespace Arch
{
    [System]
    public class CommendBufferComponentAwakeSystem : IAwake
    {
        public void Awake()
        {
            SingletonComponent.Setter<CommendBuffersComponent>((ref CommendBuffersComponent component_T1) =>
            {
                component_T1.commandBuffers = DictionaryPool<int, CommandBufferHandler>.Get();

                //因为CommendBuffer不会重复生成，因此就不考虑池化了
                foreach (var worldNamed in NamedWorld.Instance.NamedWorlds)
                {
                    component_T1.commandBuffers.Add(worldNamed.Id, new CommandBufferHandler()
                    {
                        commendBuffer = new CommandBuffer(),
                    });
                }
            });
        }
    }

    [System]
    public class CommendBufferComponentDestroySystem : IDestroy
    {
        public void Destroy()
        {
            SingletonComponent.Getter<CommendBuffersComponent>((in CommendBuffersComponent component_T1) =>
            {
                DictionaryPool<int, CommandBufferHandler>.Release(component_T1.commandBuffers);
            });
        }
    }

    [System]
    [Last]
    public class CommendBufferComponentLateUpdateSystem : ILateUpdate
    {
        public void LateUpdate()
        {
            SingletonComponent.Getter<CommendBuffersComponent>((in CommendBuffersComponent component_T1) =>
            {
                foreach (var kv in component_T1.commandBuffers)
                {
                    CommandBufferHandler pBufferHandler = kv.Value;

                    if (!pBufferHandler.isHasCommand)
                    {
                        continue;
                    }

                    int nWorldId = kv.Key;
                    World pWorld = World.Worlds[nWorldId];
                    if (pWorld == null)
                    {
                        continue;
                    }
                    pBufferHandler.commendBuffer.Playback(pWorld);
                    pBufferHandler.isHasCommand = false;
                }
            });
        }
    }
}