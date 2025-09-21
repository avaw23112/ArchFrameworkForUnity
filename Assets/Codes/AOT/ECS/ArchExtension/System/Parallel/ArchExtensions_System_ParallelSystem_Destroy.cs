using Arch.Core;
using Arch.Tools;
using Attributes;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;

//TODO:just for test
namespace Arch
{
	//public abstract class ParallelLateUpdateSystem_Test<T> : ReactiveSystem<T>, IReactiveLateUpdate
	//where T : struct, IComponent
	//{
	//	// 组件缓存（实际使用需根据框架特性调整）
	//	private NativeArray<Entity> _entities;
	//	private NativeArray<T> _components;
	//	private JobExecuteDelegate _executeDelegate;
	//	private IntPtr _executePtr;
	//	// 子类实现的业务逻辑入口
	//	protected abstract void JobExecute(in Entity entity, ref T component);
	//	protected override void Run(Entity entity, ref T component_T1)
	//	{
	//	}
	//	public void LateUpdate()
	//	{
	//		// 固定实例防止GC回收
	//		if (!_gcHandle.IsAllocated)
	//		{
	//			_gcHandle = GCHandle.Alloc(this);
	//		}

	//		world.Create<T>();
	//		world.Create<T>();
	//		world.Create<T>();

	//		QueryDescription query = Filter();
	//		int nEntityCount = world.CountEntities(query);
	//		_entities = new NativeArray<Entity>(nEntityCount, Allocator.TempJob);
	//		_components = new NativeArray<T>(nEntityCount, Allocator.TempJob);
	//		int index = 0;
	//		world.Query(in query, (Entity e, ref T c) =>
	//		{
	//			_entities[index] = e;
	//			_components[index] = c;
	//		});
	//		_executeDelegate = StaticJobExecute;
	//		_executePtr = Marshal.GetFunctionPointerForDelegate(_executeDelegate);
	//		// 创建并调度Job
	//		var job = new InternalJob
	//		{
	//			Entities = _entities,
	//			Components = _components,
	//			ExecuteLogic = new FunctionPointer<JobExecuteDelegate>(_executePtr),
	//			InstancePtr = _gcHandle.AddrOfPinnedObject(),  // 传递实例指针
	//		};


	//		job.Schedule(_entities.Length, 64).Complete();

	//		// 清理资源
	//		_entities.Dispose();
	//		_components.Dispose();
	//	}

	//	// 内部通用Job结构体
	//	private struct InternalJob : IJobParallelFor
	//	{
	//		[ReadOnly] public NativeArray<Entity> Entities;
	//		public NativeArray<T> Components;

	//		// 通过函数指针传递逻辑
	//		public FunctionPointer<JobExecuteDelegate> ExecuteLogic;
	//		public IntPtr InstancePtr;  // 新增实例指针字段
	//		public void Execute(int index)
	//		{
	//			var entity = Entities[index];
	//			var component = Components[index];
	//			ExecuteLogic.Invoke(in InstancePtr, in entity, ref component);
	//			Components[index] = component; // 回写修改
	//		}
	//	}
	//	// LateUpdate 中固定实例
	//	private GCHandle _gcHandle;  // 类成员变量
	//								 // 在子类中实现静态方法包装
	//	protected static void StaticJobExecute(in IntPtr instancePtr, in Entity entity, ref T component)
	//	{
	//		var instance = GCHandle.FromIntPtr(instancePtr).Target as ParallelLateUpdateSystem_Test<T>;
	//		instance.JobExecute(in entity, ref component);
	//	}
	//	// 修改委托定义，增加上下文指针参数
	//	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	//	public delegate void JobExecuteDelegate(in IntPtr instancePtr, in Entity entity, ref T component);

	//}
	//public struct parallelComponent : IComponent
	//{
	//	public int i;
	//}
	//[Forget]
	//public class parallelTest : ParallelLateUpdateSystem_Test<parallelComponent>
	//{
	//	protected override void JobExecute(in Entity entity, ref parallelComponent component)
	//	{
	//		ArchLog.Debug($"{component.i}");
	//	}
	//}
}
