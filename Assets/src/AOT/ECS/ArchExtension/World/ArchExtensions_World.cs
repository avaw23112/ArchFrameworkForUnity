using Arch.Buffer;
using Arch.Core;
using Arch.Tools;

namespace Arch
{
	internal static class ArchExtensions_World
	{
		public static World TakeWorld(Entity entity)
		{
			World worldEntity;
			if (!entity.isVaild())
			{
				Tools.ArchLog.LogError($"{entity} 不存在！");
				throw new System.Exception($"{entity} 不存在！");
			}
			try
			{
				worldEntity = World.Worlds[entity.WorldId];
			}
			catch
			{
				Tools.ArchLog.LogError($"不存在ID为{entity.WorldId}世界！");
				throw new System.Exception($"不存在ID为{entity.WorldId}世界！");
			}
			if (worldEntity == null)
			{
				Tools.ArchLog.LogError($"{entity} 所在的世界已经被销毁！");
				throw new System.Exception($"{entity} 所在的世界已经被销毁！");
			}
			return worldEntity;
		}

		public static CommandBuffer GetCommendBuffer(this World world)
		{
			CommendBuffersComponent commandBuffers = SingletonComponent.GetOrAdd<CommendBuffersComponent>();
			if (commandBuffers.commandBuffers == null)
			{
				ArchLog.LogError("CommendBuffersComponent 未初始化！");
				throw new System.Exception("CommendBuffersComponent 未初始化！");
			}
			CommandBufferHandler commandBuffer = commandBuffers.commandBuffers[world.Id];
			if (commandBuffer == null)
			{
				ArchLog.LogError("commandBuffer 未初始化！此世界不存在commandBuffer！");
				throw new System.Exception("commandBuffer 未初始化！此世界不存在commandBuffer！");
			}
			commandBuffer.isHasCommand = true;
			return commandBuffer.commendBuffer;
		}
	}
}