using Arch;
using Arch.Tools;
using Arch.Tools.Pool;
using System;
using System.Collections.Generic;

namespace Attributes
{
	public class Attributes : Singleton<Attributes>
	{
		/// <summary>
		/// [Attribute BaseAttribute Type, Value :[Decrect Type,Decrect Attribute Value]
		/// </summary>
		private Dictionary<Type, Dictionary<Type, List<object>>> m_dicAttributeToDecrectType;

		public Attributes()
		{
			m_dicAttributeToDecrectType = new Dictionary<Type, Dictionary<Type, List<object>>>();
		}

		/// <summary>
		/// 检查标记类上是否带有Forget属性
		/// </summary>
		public static bool isForget(Type derectType)
		{
			if (derectType == null)
			{
				throw new NullReferenceException("derectType is null");
			}

			object[] attributeTypes = derectType.GetCustomAttributes(typeof(ForgetAttribute), false);
			return attributeTypes.Length > 0;
		}

		/// <summary>
		/// 检查标记类上是否带有isNotHotReload属性
		/// </summary>
		public static bool isNotHotReload(Type derectType)
		{
			if (derectType == null)
			{
				throw new NullReferenceException("derectType is null");
			}

			object[] attributeTypes = derectType.GetCustomAttributes(typeof(NotHotReloadAttribute), false);
			return attributeTypes.Length > 0;
		}

		public static void RegisterHotReloadableAttributeSystems()
		{

			List<Type> processors = ListPool<Type>.Get();
			Collector.CollectTypes<IAttributeProcessor>(processors);
			try
			{
				foreach (var processor in processors)
				{
					if (processor.IsAbstract)
					{
						continue;
					}
					if (isForget(processor))
					{
						continue;
					}
					if (isNotHotReload(processor))
					{
						continue;
					}
					if (Activator.CreateInstance(processor) is IAttributeProcessor processorInstance)
					{
						processorInstance.Process();
					}
				}
			}
			catch (Exception e)
			{
				ArchLog.LogError(e.Message);
				throw;
			}
			finally
			{
				ListPool<Type>.Release(processors);
			}
		}

		public static void RegisterAttributeSystems()
		{
			List<Type> processors = ListPool<Type>.Get();
			Collector.CollectTypesParallel<IAttributeProcessor>(processors);
			try
			{
				foreach (var processor in processors)
				{
					if (processor.IsAbstract)
					{
						continue;
					}
					if (isForget(processor))
					{
						continue;
					}
					if (Activator.CreateInstance(processor) is IAttributeProcessor processorInstance)
					{
						processorInstance.Process();
					}
				}
			}
			catch (Exception e)
			{
				ArchLog.LogError(e.Message);
				throw;
			}
			finally
			{
				ListPool<Type>.Release(processors);
			}
		}
		public static void RemoveMapping()
		{
			Instance.m_dicAttributeToDecrectType.Clear();
		}
		public static void AddMapping(Type attributeType, object attributeValue, Type decrectType)
		{
			Dictionary<Type, List<object>> dicTypes;
			if (!Instance.m_dicAttributeToDecrectType.TryGetValue(attributeType, out dicTypes))
			{
				dicTypes = new Dictionary<Type, List<object>>();
				Instance.m_dicAttributeToDecrectType.Add(attributeType, dicTypes);
			}
			List<object> listAttributeObject;
			if (!dicTypes.TryGetValue(decrectType, out listAttributeObject))
			{
				listAttributeObject = new List<object>();
				dicTypes.Add(decrectType, listAttributeObject);
			}
			listAttributeObject.Add(attributeValue);
		}

		public static bool TryGetDecrectType(Type attributeType, out Dictionary<Type, List<object>> decrectType)
		{
			bool isSuccess = HasMapping(attributeType);
			decrectType = isSuccess ? Instance.m_dicAttributeToDecrectType[attributeType] : null;
			return isSuccess;
		}

		public static bool HasBaseMapping()
		{
			return Instance.m_dicAttributeToDecrectType.Count > 0;
		}

		public static bool HasMapping(Type attributeType)
		{
			return Instance.m_dicAttributeToDecrectType.ContainsKey(attributeType);
		}
	}
}