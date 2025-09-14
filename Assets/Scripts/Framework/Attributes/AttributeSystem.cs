using System;
using System.Collections.Generic;
using Tools.Pool;

namespace Attributes
{
	public interface IAttributeProcessor
	{
		void Process();
	}

	public abstract class MutipleAttributeSystem<T> : IAttributeProcessor where T : BaseAttribute
	{
		public void Process()
		{
			Dictionary<Type, List<T>> attributes = DictionaryPool<Type, List<T>>.Get();
			Collector.CollectAttributesMulti<T>(attributes);
			foreach (var kvp in attributes)
			{
				Type directType = kvp.Key;
				var list = kvp.Value;
				Process(directType, list);
			}
			DictionaryPool<Type, List<T>>.Release(attributes);
		}

		public abstract void Process(Type directType, List<T> list_T);
	}

	public abstract class MutipleAttributeSystem<T1, T2> : IAttributeProcessor
		where T1 : BaseAttribute
		where T2 : BaseAttribute
	{
		public void Process()
		{
			Dictionary<Type, (List<T1>, List<T2>)> attributes = DictionaryPool<Type, (List<T1>, List<T2>)>.Get();
			Collector.CollectAttributesMulti<T1, T2>(attributes);
			foreach (var kvp in attributes)
			{
				Type derectType = kvp.Key;
				var (list_T1, list_T2) = kvp.Value;
				Process(derectType, list_T1, list_T2);
			}
			DictionaryPool<Type, (List<T1>, List<T2>)>.Release(attributes);
		}

		public abstract void Process(Type directType, List<T1> list_T1, List<T2> list_T2);
	}

	public abstract class MutipleAttributeSystem<T1, T2, T3> : IAttributeProcessor
		where T1 : BaseAttribute
		where T2 : BaseAttribute
		where T3 : BaseAttribute
	{
		public void Process()
		{
			Dictionary<Type, (List<T1>, List<T2>, List<T3>)> attributes = DictionaryPool<Type, (List<T1>, List<T2>, List<T3>)>.Get();
			Collector.CollectAttributesMulti<T1, T2, T3>(attributes);
			foreach (var kvp in attributes)
			{
				Type directType = kvp.Key;
				var (list_T1, list_T2, list_T3) = kvp.Value;
				Process(directType, list_T1, list_T2, list_T3);
			}
			DictionaryPool<Type, (List<T1>, List<T2>, List<T3>)>.Release(attributes);
		}

		public abstract void Process(Type directType, List<T1> list_T1, List<T2> list_T2, List<T3> list_T3);
	}

	public abstract class AttributeSystem<T> : IAttributeProcessor where T : BaseAttribute
	{
		public void Process()
		{
			List<(T, Type)> attributes = ListPool<(T, Type)>.Get();
			Collector.CollectAttributes<T>(attributes);
			foreach ((T attribute, Type derectType) in attributes)
			{
				Process(attribute, derectType);
			}
			ListPool<(T, Type)>.Release(attributes);
		}

		public abstract void Process(T attribute, Type directType);
	}

	public abstract class AttributeSystem<T1, T2> : IAttributeProcessor
		where T1 : BaseAttribute
		where T2 : BaseAttribute
	{
		public void Process()
		{
			List<(T1, T2, Type)> attributes = ListPool<(T1, T2, Type)>.Get();
			Collector.CollectAttributes<T1, T2>(attributes);
			foreach ((T1 attribute, T2 attribute2, Type directType) in attributes)
			{
				Process(attribute, attribute2, directType);
			}
			ListPool<(T1, T2, Type)>.Release(attributes);
		}

		public abstract void Process(T1 attribute_T1, T2 attribute_T2, Type directType);
	}

	public abstract class AttributeSystem<T1, T2, T3> : IAttributeProcessor
	where T1 : BaseAttribute
	where T2 : BaseAttribute
	where T3 : BaseAttribute
	{
		public void Process()
		{
			List<(T1, T2, T3, Type)> attributes = ListPool<(T1, T2, T3, Type)>.Get();
			Collector.CollectAttributes<T1, T2, T3>(attributes);
			foreach ((T1 attribute, T2 attribute2, T3 attribute3, Type derectType) in attributes)
			{
				Process(attribute, attribute2, attribute3, derectType);
			}
			ListPool<(T1, T2, T3, Type)>.Release(attributes);
		}

		public abstract void Process(T1 attribute_T1, T2 attribute_T2, T3 attribute_T3, Type derectType);
	}
}