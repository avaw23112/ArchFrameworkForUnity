using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"AOT.dll",
		"Arch.LowLevel.dll",
		"Arch.dll",
		"CommunityToolkit.HighPerformance.dll",
		"MemoryPack.Core.dll",
		"System.Core.dll",
		"System.Runtime.CompilerServices.Unsafe.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Arch.AwakeSystem<Arch.StateMachineComponent>
	// Arch.Core.Events.ComponentAddedHandler<Arch.HotReloadTest_Model>
	// Arch.Core.Events.ComponentAddedHandler<Arch.PlayerStatePriorityTreeComponent>
	// Arch.Core.Events.ComponentAddedHandler<Arch.StateMachineComponent>
	// Arch.Core.Events.ComponentRemovedHandler<Arch.PlayerStatePriorityTreeComponent>
	// Arch.Core.Events.ComponentRemovedHandler<Arch.StateMachineComponent>
	// Arch.Core.Events.Events<Arch.HotReloadTest_Model>
	// Arch.Core.Events.Events<Arch.PlayerStatePriorityTreeComponent>
	// Arch.Core.ForEachWithEntity<Arch.HotReloadTest_Model>
	// Arch.Core.ForEachWithEntity<Arch.StateMachineComponent>
	// Arch.DestroySystem<Arch.PlayerStatePriorityTreeComponent>
	// Arch.DestroySystem<Arch.StateMachineComponent>
	// Arch.LateUpdateSystem<Arch.StateMachineComponent>
	// Arch.LowLevel.Array<Arch.LowLevel.Jagged.Bucket<Arch.Core.EntityData>>
	// Arch.LowLevel.Enumerator<Arch.LowLevel.Jagged.Bucket<Arch.Core.EntityData>>
	// Arch.LowLevel.Jagged.Bucket<Arch.Core.EntityData>
	// Arch.LowLevel.Jagged.JaggedArray<Arch.Core.EntityData>
	// Arch.ReactiveSystem<Arch.HotReloadTest_Model>
	// Arch.ReactiveSystem<Arch.PlayerStatePriorityTreeComponent>
	// Arch.ReactiveSystem<Arch.StateMachineComponent>
	// Arch.Tools.Pool.QueuePool<Arch.StateChangeEvent>
	// Arch.UniqueComponentSystem<Arch.PlayerStatePriorityTreeComponent>
	// Arch.UpdateSystem<Arch.HotReloadTest_Model>
	// Events.Event<Events.GameStartEvent>
	// MemoryPack.Formatters.ArrayFormatter<Arch.HotReloadTest_Model>
	// MemoryPack.IMemoryPackable<Arch.HotReloadTest_Model>
	// MemoryPack.MemoryPackFormatter<Arch.HotReloadTest_Model>
	// MemoryPack.MemoryPackFormatter<object>
	// RefEvent.RefAction<Arch.PlayerStatePriorityTreeComponent>
	// System.Action<Events.GameStartEvent>
	// System.Action<object>
	// System.Buffers.IBufferWriter<byte>
	// System.ByReference<Arch.LowLevel.Jagged.Bucket<Arch.Core.EntityData>>
	// System.ByReference<byte>
	// System.ByReference<ushort>
	// System.Collections.Concurrent.ConcurrentDictionary.<GetEnumerator>d__35<object,object>
	// System.Collections.Concurrent.ConcurrentDictionary.DictionaryEnumerator<object,object>
	// System.Collections.Concurrent.ConcurrentDictionary.Node<object,object>
	// System.Collections.Concurrent.ConcurrentDictionary.Tables<object,object>
	// System.Collections.Concurrent.ConcurrentDictionary<object,object>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.ComparisonComparer<object>
	// System.Collections.Generic.Dictionary.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,Arch.Core.Entity>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,Arch.Core.Entity>
	// System.Collections.Generic.Dictionary.KeyCollection<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,Arch.Core.Entity>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,Arch.Core.Entity>
	// System.Collections.Generic.Dictionary.ValueCollection<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,Arch.Core.Entity>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<object,Arch.Core.Entity>
	// System.Collections.Generic.EqualityComparer<Arch.Core.Entity>
	// System.Collections.Generic.EqualityComparer<Arch.Core.EntityData>
	// System.Collections.Generic.EqualityComparer<int>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.HashSet.Enumerator<object>
	// System.Collections.Generic.HashSet<object>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,Arch.Core.Entity>>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IDictionary<object,object>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,Arch.Core.Entity>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,Arch.Core.Entity>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<int>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.KeyValuePair<int,object>
	// System.Collections.Generic.KeyValuePair<object,Arch.Core.Entity>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<Arch.Core.Entity>
	// System.Collections.Generic.ObjectEqualityComparer<Arch.Core.EntityData>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.Queue.Enumerator<Arch.Core.RecycledEntity>
	// System.Collections.Generic.Queue.Enumerator<Arch.StateChangeEvent>
	// System.Collections.Generic.Queue<Arch.Core.RecycledEntity>
	// System.Collections.Generic.Queue<Arch.StateChangeEvent>
	// System.Collections.Generic.Stack.Enumerator<object>
	// System.Collections.Generic.Stack<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<object>
	// System.Func<object,object,object>
	// System.Func<object,object>
	// System.Predicate<object>
	// System.ReadOnlySpan<Arch.LowLevel.Jagged.Bucket<Arch.Core.EntityData>>
	// System.ReadOnlySpan<byte>
	// System.ReadOnlySpan<ushort>
	// System.Span.Enumerator<Arch.LowLevel.Jagged.Bucket<Arch.Core.EntityData>>
	// System.Span.Enumerator<byte>
	// System.Span.Enumerator<ushort>
	// System.Span<Arch.LowLevel.Jagged.Bucket<Arch.Core.EntityData>>
	// System.Span<byte>
	// System.Span<ushort>
	// }}

	public void RefMethods()
	{
		// int Arch.Core.Archetype.Add<Arch.HotReloadTest_Model>(Arch.Core.Entity,Arch.Core.Slot&,Arch.HotReloadTest_Model&)
		// int Arch.Core.Archetype.Add<Arch.PlayerStatePriorityTreeComponent>(Arch.Core.Entity,Arch.Core.Slot&,Arch.PlayerStatePriorityTreeComponent&)
		// Arch.HotReloadTest_Model& Arch.Core.Archetype.Get<Arch.HotReloadTest_Model>(Arch.Core.Slot&)
		// Arch.PlayerStatePriorityTreeComponent& Arch.Core.Archetype.Get<Arch.PlayerStatePriorityTreeComponent>(Arch.Core.Slot&)
		// bool Arch.Core.Archetype.Has<Arch.PlayerStatePriorityTreeComponent>()
		// bool Arch.Core.Archetype.Has<Arch.PlayerTag>()
		// System.Void Arch.Core.Chunk.Copy<Arch.HotReloadTest_Model>(int,Arch.HotReloadTest_Model&)
		// System.Void Arch.Core.Chunk.Copy<Arch.PlayerStatePriorityTreeComponent>(int,Arch.PlayerStatePriorityTreeComponent&)
		// Arch.HotReloadTest_Model& Arch.Core.Chunk.Get<Arch.HotReloadTest_Model>(int)
		// Arch.PlayerStatePriorityTreeComponent& Arch.Core.Chunk.Get<Arch.PlayerStatePriorityTreeComponent>(int)
		// Arch.HotReloadTest_Model[] Arch.Core.Chunk.GetArray<Arch.HotReloadTest_Model>()
		// Arch.PlayerStatePriorityTreeComponent[] Arch.Core.Chunk.GetArray<Arch.PlayerStatePriorityTreeComponent>()
		// Arch.HotReloadTest_Model& Arch.Core.Chunk.GetFirst<Arch.HotReloadTest_Model>()
		// Arch.PlayerStatePriorityTreeComponent& Arch.Core.Chunk.GetFirst<Arch.PlayerStatePriorityTreeComponent>()
		// int Arch.Core.Chunk.Index<Arch.HotReloadTest_Model>()
		// int Arch.Core.Chunk.Index<Arch.PlayerStatePriorityTreeComponent>()
		// Arch.PlayerStatePriorityTreeComponent& Arch.Core.Extensions.EntityExtensions.Get<Arch.PlayerStatePriorityTreeComponent>(Arch.Core.Entity&)
		// bool Arch.Core.Extensions.EntityExtensions.Has<Arch.PlayerStatePriorityTreeComponent>(Arch.Core.Entity&)
		// bool Arch.Core.Extensions.EntityExtensions.Has<Arch.PlayerTag>(Arch.Core.Entity&)
		// Arch.Core.Entity Arch.Core.World.Create<Arch.HotReloadTest_Model>(Arch.HotReloadTest_Model&)
		// Arch.Core.Entity Arch.Core.World.Create<Arch.PlayerStatePriorityTreeComponent>(Arch.PlayerStatePriorityTreeComponent&)
		// Arch.HotReloadTest_Model& Arch.Core.World.Get<Arch.HotReloadTest_Model>(Arch.Core.Entity)
		// Arch.PlayerStatePriorityTreeComponent& Arch.Core.World.Get<Arch.PlayerStatePriorityTreeComponent>(Arch.Core.Entity)
		// Arch.Core.Events.Events<Arch.HotReloadTest_Model>& modreq(System.Runtime.InteropServices.InAttribute) Arch.Core.World.GetEvents<Arch.HotReloadTest_Model>()
		// Arch.Core.Events.Events<Arch.PlayerStatePriorityTreeComponent>& modreq(System.Runtime.InteropServices.InAttribute) Arch.Core.World.GetEvents<Arch.PlayerStatePriorityTreeComponent>()
		// bool Arch.Core.World.Has<Arch.PlayerStatePriorityTreeComponent>(Arch.Core.Entity)
		// bool Arch.Core.World.Has<Arch.PlayerTag>(Arch.Core.Entity)
		// System.Void Arch.Core.World.OnComponentAdded<Arch.HotReloadTest_Model>(Arch.Core.Entity)
		// System.Void Arch.Core.World.OnComponentAdded<Arch.PlayerStatePriorityTreeComponent>(Arch.Core.Entity)
		// Arch.PlayerStatePriorityTreeComponent Arch.SingletonComponent.GetOrAdd<Arch.PlayerStatePriorityTreeComponent>()
		// Arch.HotReloadTest_Model& CommunityToolkit.HighPerformance.ArrayExtensions.DangerousGetReference<Arch.HotReloadTest_Model>(Arch.HotReloadTest_Model[])
		// Arch.PlayerStatePriorityTreeComponent& CommunityToolkit.HighPerformance.ArrayExtensions.DangerousGetReference<Arch.PlayerStatePriorityTreeComponent>(Arch.PlayerStatePriorityTreeComponent[])
		// int& CommunityToolkit.HighPerformance.ArrayExtensions.DangerousGetReferenceAt<int>(int[],int)
		// object& CommunityToolkit.HighPerformance.ArrayExtensions.DangerousGetReferenceAt<object>(object[],int)
		// System.IntPtr CommunityToolkit.HighPerformance.Helpers.Internals.RuntimeHelpers.GetArrayDataByteOffset<Arch.HotReloadTest_Model>()
		// System.IntPtr CommunityToolkit.HighPerformance.Helpers.Internals.RuntimeHelpers.GetArrayDataByteOffset<Arch.PlayerStatePriorityTreeComponent>()
		// System.IntPtr CommunityToolkit.HighPerformance.Helpers.Internals.RuntimeHelpers.GetArrayDataByteOffset<int>()
		// System.IntPtr CommunityToolkit.HighPerformance.Helpers.Internals.RuntimeHelpers.GetArrayDataByteOffset<object>()
		// Arch.HotReloadTest_Model& CommunityToolkit.HighPerformance.Helpers.ObjectMarshal.DangerousGetObjectDataReferenceAt<Arch.HotReloadTest_Model>(object,System.IntPtr)
		// Arch.PlayerStatePriorityTreeComponent& CommunityToolkit.HighPerformance.Helpers.ObjectMarshal.DangerousGetObjectDataReferenceAt<Arch.PlayerStatePriorityTreeComponent>(object,System.IntPtr)
		// int& CommunityToolkit.HighPerformance.Helpers.ObjectMarshal.DangerousGetObjectDataReferenceAt<int>(object,System.IntPtr)
		// object& CommunityToolkit.HighPerformance.Helpers.ObjectMarshal.DangerousGetObjectDataReferenceAt<object>(object,System.IntPtr)
		// bool MemoryPack.MemoryPackFormatterProvider.IsRegistered<Arch.HotReloadTest_Model>()
		// bool MemoryPack.MemoryPackFormatterProvider.IsRegistered<object>()
		// System.Void MemoryPack.MemoryPackFormatterProvider.Register<Arch.HotReloadTest_Model>(MemoryPack.MemoryPackFormatter<Arch.HotReloadTest_Model>)
		// System.Void MemoryPack.MemoryPackFormatterProvider.Register<object>(MemoryPack.MemoryPackFormatter<object>)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<Arch.HotReloadTest_Model>(Arch.HotReloadTest_Model&)
		// System.Void MemoryPack.MemoryPackWriter<object>.WriteUnmanaged<Arch.HotReloadTest_Model>(Arch.HotReloadTest_Model&)
		// System.Void System.Array.Resize<object>(object[]&,int)
		// Arch.HotReloadTest_Model& System.Runtime.CompilerServices.Unsafe.Add<Arch.HotReloadTest_Model>(Arch.HotReloadTest_Model&,int)
		// Arch.PlayerStatePriorityTreeComponent& System.Runtime.CompilerServices.Unsafe.Add<Arch.PlayerStatePriorityTreeComponent>(Arch.PlayerStatePriorityTreeComponent&,int)
		// int& System.Runtime.CompilerServices.Unsafe.Add<int>(int&,System.IntPtr)
		// object& System.Runtime.CompilerServices.Unsafe.Add<object>(object&,System.IntPtr)
		// byte& System.Runtime.CompilerServices.Unsafe.AddByteOffset<byte>(byte&,System.IntPtr)
		// Arch.HotReloadTest_Model& System.Runtime.CompilerServices.Unsafe.As<byte,Arch.HotReloadTest_Model>(byte&)
		// Arch.PlayerStatePriorityTreeComponent& System.Runtime.CompilerServices.Unsafe.As<byte,Arch.PlayerStatePriorityTreeComponent>(byte&)
		// int& System.Runtime.CompilerServices.Unsafe.As<byte,int>(byte&)
		// object& System.Runtime.CompilerServices.Unsafe.As<byte,object>(byte&)
		// object& System.Runtime.CompilerServices.Unsafe.As<object,object>(object&)
		// Arch.HotReloadTest_Model System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Arch.HotReloadTest_Model>(byte&)
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Arch.HotReloadTest_Model>()
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Arch.HotReloadTest_Model>(byte&,Arch.HotReloadTest_Model)
	}
}