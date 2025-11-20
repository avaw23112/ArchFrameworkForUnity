using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"ArchFramework.Runtime.dll",
		"MemoryPack.Core.dll",
		"System.Runtime.CompilerServices.Unsafe.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Events.Event<Events.GameStartEvent>
	// MemoryPack.Formatters.ArrayFormatter<Arch.HotReloadTest_Model>
	// MemoryPack.IMemoryPackable<Arch.HotReloadTest_Model>
	// MemoryPack.MemoryPackFormatter<Arch.HotReloadTest_Model>
	// MemoryPack.MemoryPackFormatter<object>
	// System.Action<Events.GameStartEvent>
	// System.Action<object>
	// System.Buffers.IBufferWriter<byte>
	// System.ByReference<byte>
	// System.ByReference<ushort>
	// System.Collections.Concurrent.ConcurrentDictionary.<GetEnumerator>d__35<object,object>
	// System.Collections.Concurrent.ConcurrentDictionary.DictionaryEnumerator<object,object>
	// System.Collections.Concurrent.ConcurrentDictionary.Node<object,object>
	// System.Collections.Concurrent.ConcurrentDictionary.Tables<object,object>
	// System.Collections.Concurrent.ConcurrentDictionary<object,object>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IDictionary<object,object>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<object>
	// System.Func<object,object>
	// System.Predicate<object>
	// System.ReadOnlySpan<byte>
	// System.ReadOnlySpan<ushort>
	// System.Span.Enumerator<byte>
	// System.Span.Enumerator<ushort>
	// System.Span<byte>
	// System.Span<ushort>
	// }}

	public void RefMethods()
	{
		// bool MemoryPack.MemoryPackFormatterProvider.IsRegistered<Arch.HotReloadTest_Model>()
		// bool MemoryPack.MemoryPackFormatterProvider.IsRegistered<object>()
		// System.Void MemoryPack.MemoryPackFormatterProvider.Register<Arch.HotReloadTest_Model>(MemoryPack.MemoryPackFormatter<Arch.HotReloadTest_Model>)
		// System.Void MemoryPack.MemoryPackFormatterProvider.Register<object>(MemoryPack.MemoryPackFormatter<object>)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<Arch.HotReloadTest_Model>(Arch.HotReloadTest_Model&)
		// System.Void MemoryPack.MemoryPackWriter<object>.WriteUnmanaged<Arch.HotReloadTest_Model>(Arch.HotReloadTest_Model&)
		// object& System.Runtime.CompilerServices.Unsafe.As<object,object>(object&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<object>(object&)
		// Arch.HotReloadTest_Model System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Arch.HotReloadTest_Model>(byte&)
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Arch.HotReloadTest_Model>()
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Arch.HotReloadTest_Model>(byte&,Arch.HotReloadTest_Model)
	}
}