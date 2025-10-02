using Arch.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Attributes
{
	///
	///
	///
	public static class Collector
	{
		private static readonly object _attributeLock = new object();

		#region Attributes

		///
		///
		///
		public static bool isForget(Type derectType)
		{
			if (derectType == null)
			{
				throw new NullReferenceException("derectType is null");
			}

			object[] attributeTypes = derectType.GetCustomAttributes(typeof(ForgetAttribute), false);
			return attributeTypes.Length > 0;
		}

		///
		///
		///
		public static bool isCollectable<T>(Type derectType)
			where T : Attribute
		{
			if (derectType == null)
			{
				throw new NullReferenceException("derectType is null");
			}
			object[] attributeTypes = derectType.GetCustomAttributes(typeof(BaseCollectableAttribute), false);
			foreach (var attribute in attributeTypes)
			{
				var baseCollectableAttribute = attribute as BaseCollectableAttribute;
				if (baseCollectableAttribute == null)
				{
					continue;
				}

				//
				if (baseCollectableAttribute.GetType() == typeof(T))
				{
					continue;
				}
				else
				{
					return true;
				}
			}
			return false;
		}

		///
		///
		///
		private static bool isCollectable<T1, T2>(Type derectType)
			where T1 : Attribute
			where T2 : Attribute
		{
			if (derectType == null)
			{
				throw new NullReferenceException("derectType is null");
			}
			object[] attributeTypes = derectType.GetCustomAttributes(typeof(BaseCollectableAttribute), false);
			foreach (var attribute in attributeTypes)
			{
				var baseCollectableAttribute = attribute as BaseCollectableAttribute;
				if (baseCollectableAttribute == null)
				{
					continue;
				}
				Type actualType = baseCollectableAttribute.GetType();
				if (actualType == typeof(T1))
				{
					continue;
				}
				else if (actualType == typeof(T2))
				{
					continue;
				}
				else
				{
					return true;
				}
			}
			return false;
		}

		///
		///
		///
		private static bool isCollectable<T1, T2, T3>(Type derectType)
			where T1 : Attribute
			where T2 : Attribute
			where T3 : Attribute
		{
			if (derectType == null)
			{
				throw new NullReferenceException("derectType is null");
			}
			object[] attributeTypes = derectType.GetCustomAttributes(typeof(BaseCollectableAttribute), false);
			foreach (var attribute in attributeTypes)
			{
				var baseCollectableAttribute = attribute as BaseCollectableAttribute;
				if (baseCollectableAttribute == null)
				{
					continue;
				}
				Type actualType = baseCollectableAttribute.GetType();
				if (actualType == typeof(T1))
				{
					continue;
				}
				else if (actualType == typeof(T2))
				{
					continue;
				}
				else if (actualType == typeof(T3))
				{
					continue;
				}
				else
				{
					return true;
				}
			}
			return false;
		}


		#endregion Attributes

		public static void CollectTypes<T>(List<Type> listTypes) where T : class
		{
			if (listTypes == null)
			{
				throw new ArgumentNullException(nameof(listTypes));
			}

			//
			foreach (var assembly in Assemblys.AllAssemblies)
			{
				Type[] types = assembly.GetTypes();
				foreach (var type in types)
				{
					if (isForget(type)) return;
					if (typeof(T).IsAssignableFrom(type))
					{
						listTypes.Add(type);
					}
					else if (type.IsSubclassOf(typeof(T)))
					{
						listTypes.Add(type);
					}
				}
			}
		}

		public static void CollectBaseAttributes()
		{

			//
			if (Attributes.HasBaseMapping()) return;

			//
			foreach (var assembly in Assemblys.AllAssemblies)
			{
				Type[] types = assembly.GetTypes();
				foreach (var type in types)
				{
					if (!type.IsClass || type.IsEnum)
					{
						continue;
					}
					if (isForget(type))
					{
						continue;
					}
					object[] attributeTypes = type.GetCustomAttributes(typeof(BaseAttribute), false);
					if (attributeTypes.Length <= 0)
					{
						continue;
					}
					foreach (var attribute in attributeTypes)
					{
						Attributes.AddMapping(attribute.GetType(), attribute, type);
					}
				}
			}
		}

		public static void CollectBaseAttributesParallel()
		{

			if (Attributes.HasBaseMapping()) return;

			Parallel.ForEach(Assemblys.AllAssemblies, assembly =>
			{
				Type[] types = assembly.GetTypes();

				var localAttributes = new ConcurrentBag<(Type, BaseAttribute, Type)>();

				Parallel.ForEach(types, type =>
				{
					if (type.IsAbstract || type.IsInterface || type.IsEnum || isForget(type)) return;
					var attrs = type.GetCustomAttributes(typeof(BaseAttribute), false);
					if (attrs.Length == 0) return;

					foreach (BaseAttribute attr in attrs)
					{
						localAttributes.Add((attr.GetType(), attr, type));
					}
				});

				if (localAttributes.Count > 0)
				{
					lock (_attributeLock)
					{
						foreach (var item in localAttributes)
						{
							Attributes.AddMapping(item.Item1, item.Item2, item.Item3);
						}
					}
				}
			});
		}

		//
		public static void CollectTypesParallel<T>(List<Type> listTypes) where T : class
		{
			if (listTypes == null) throw new ArgumentNullException(nameof(listTypes));

			var concurrentTypes = new ConcurrentBag<Type>();
			var targetType = typeof(T);

			Parallel.ForEach(Assemblys.AllAssemblies, assembly =>
			{
				Type[] types = assembly.GetTypes();

				Parallel.ForEach(types, type =>
				{
					if (isForget(type))
						return;
					if (targetType.IsAssignableFrom(type) || type.IsSubclassOf(targetType))
					{
						concurrentTypes.Add(type);
					}
				});
			});

			//
			var uniqueTypes = concurrentTypes.Distinct().ToList();
			listTypes.AddRange(uniqueTypes);
		}

		public static void CollectAttributes<T>(List<(T, Type)> listAttributes) where T : Attribute
		{
			if (listAttributes == null)
			{
				throw new ArgumentNullException(nameof(listAttributes));
			}



			//
			Dictionary<Type, List<object>> dictAttributes;
			if (Attributes.TryGetDecrectType(typeof(T), out dictAttributes))
			{
				//
				if (dictAttributes == null)
				{
					return;
				}
				foreach (var item in dictAttributes)
				{
					Type pDerectType = item.Key;
					List<object> listDerectAttribute = item.Value;
					if (pDerectType.IsAbstract || pDerectType.IsInterface
						|| isForget(pDerectType) || isCollectable<T>(pDerectType))
					{
						continue;
					}
					if (item.Value.Count > 1)
					{
						Arch.Tools.ArchLog.LogDebug("debug");
						continue;
					}
					if (item.Value.Count <= 0)
					{
						throw new ArgumentException("attribute is null");
					}
					T attribute = (T)item.Value[0];
					if (attribute == null)
					{
						throw new ArgumentException("attribute is null");
					}
					listAttributes.Add((attribute, pDerectType));
				}
			}
		}

		public static void CollectAttributes<T1, T2>(List<(T1, T2, Type)> listAttributes)
			where T1 : Attribute
			where T2 : Attribute
		{
			if (listAttributes == null)
			{
				throw new ArgumentNullException(nameof(listAttributes));
			}

			CollectBaseAttributesParallel();
			//
			Dictionary<Type, List<object>> dictAttributes;
			if (Attributes.TryGetDecrectType(typeof(T1), out dictAttributes))
			{
				//
				if (dictAttributes == null)
				{
					return;
				}
				foreach (var item in dictAttributes)
				{
					Type pDerectType = item.Key;
					List<object> listDerectAttribute = item.Value;
					if (pDerectType.IsAbstract || pDerectType.IsInterface)
					{
						continue;
					}
					if (isForget(pDerectType))
					{
						continue;
					}
					if (isCollectable<T1, T2>(pDerectType))
					{
						continue;
					}
					if (item.Value.Count > 1)
					{
						Arch.Tools.ArchLog.LogDebug("debug");
						continue;
					}
					if (item.Value.Count <= 0)
					{
						throw new ArgumentException("attribute is null");
					}
					T1 attribute = (T1)item.Value[0];
					if (attribute == null)
					{
						throw new ArgumentException("attribute is null");
					}

					object[] attributeTypes = pDerectType.GetCustomAttributes(typeof(T2), false);
					//
					if (attributeTypes.Length <= 0)
					{
						continue;
					}
					foreach (var attribute2 in attributeTypes)
					{
						T2 attribute2T = attribute2 as T2;
						if (attribute2T != null)
						{
							//
							listAttributes.Add((attribute, attribute2T, pDerectType));
							continue;
						}
					}
				}
			}
		}

		internal static void CollectAttributes<T1, T2, T3>(List<ValueTuple<T1, T2, T3, Type>> listAttributes)
			where T1 : Attribute
			where T2 : Attribute
			where T3 : Attribute
		{
			if (listAttributes == null)
			{
				throw new ArgumentNullException(nameof(listAttributes));
			}
			//
			Dictionary<Type, List<object>> dictAttributes;
			if (Attributes.TryGetDecrectType(typeof(T1), out dictAttributes))
			{
				//
				if (dictAttributes == null)
				{
					return;
				}
				foreach (var item in dictAttributes)
				{
					Type pDerectType = item.Key;
					List<object> listDerectAttribute = item.Value;
					if (pDerectType.IsAbstract || pDerectType.IsInterface)
					{
						continue;
					}
					if (isForget(pDerectType))
					{
						continue;
					}
					if (isCollectable<T1, T2, T3>(pDerectType))
					{
						continue;
					}
					if (item.Value.Count > 1)
					{
						Arch.Tools.ArchLog.LogDebug("debug");
						continue;
					}
					if (item.Value.Count <= 0)
					{
						throw new ArgumentException("attribute is null");
					}
					T1 attribute = (T1)item.Value[0];
					if (attribute == null)
					{
						throw new ArgumentException("attribute is null");
					}

					object[] attributeTypes_T2 = pDerectType.GetCustomAttributes(typeof(T2), false);
					object[] attributeTypes_T3 = pDerectType.GetCustomAttributes(typeof(T3), false);
					//
					if (attributeTypes_T2.Length <= 0 || attributeTypes_T3.Length <= 0)
					{
						continue;
					}
					if (attributeTypes_T2.Length > 1 || attributeTypes_T3.Length > 1)
					{
						continue;
					}
					T2 attribute2T = attributeTypes_T2[0] as T2;
					T3 attribute3T = attributeTypes_T3[0] as T3;

					if (attribute2T == null || attribute3T == null)
					{
						ArchLog.LogError("������󣡲��񵽷���Ч����?");
						throw new Exception("������󣡲��񵽷���Ч���ԣ�?");
					}
					listAttributes.Add((attribute, attribute2T, attribute3T, pDerectType));
				}
			}
		}

		public static void CollectAttributesMulti<T1>(Dictionary<Type, List<T1>> dictAttributesMulti)
			where T1 : Attribute
		{
			if (dictAttributesMulti == null)
			{
				throw new ArgumentNullException(nameof(dictAttributesMulti));
			}

			//
			Dictionary<Type, List<object>> dictAttributes;
			if (Attributes.TryGetDecrectType(typeof(T1), out dictAttributes))
			{
				//
				if (dictAttributes == null)
				{
					return;
				}
				foreach (var item in dictAttributes)
				{
					Type pDerectType = item.Key;
					if (isForget(pDerectType))
					{
						continue;
					}
					if (isCollectable<T1>(pDerectType))
					{
						continue;
					}
					if (item.Value.Count <= 0)
					{
						throw new ArgumentException("attribute is null");
					}
					List<T1> pList_T1 = item.Value.Cast<T1>().ToList();
					dictAttributesMulti.Add(pDerectType, pList_T1);
				}
			}
		}

		public static void CollectAttributesMulti<T1, T2>(Dictionary<Type, (List<T1>, List<T2>)> dictAttributesMulti)
			where T1 : Attribute
			where T2 : Attribute
		{
			if (dictAttributesMulti == null)
			{
				throw new ArgumentNullException(nameof(dictAttributesMulti));
			}

			//
			Dictionary<Type, List<object>> dictAttributes_T1;
			Dictionary<Type, List<object>> dictAttributes_T2;
			Attributes.TryGetDecrectType(typeof(T1), out dictAttributes_T1);
			Attributes.TryGetDecrectType(typeof(T2), out dictAttributes_T2);

			//
			if (dictAttributes_T1 == null || dictAttributes_T2 == null)
			{
				return;
			}

			foreach (var item in dictAttributes_T1)
			{
				Type pDerectType = item.Key;
				if (isForget(pDerectType))
				{
					continue;
				}
				if (isCollectable<T1, T2>(pDerectType))
				{
					continue;
				}
				if (item.Value.Count <= 0)
				{
					throw new ArgumentException("attribute is null");
				}
				if (dictAttributes_T2.TryGetValue(pDerectType, out var Value_T2))
				{
					List<T1> pList_T1 = item.Value.Cast<T1>().ToList();
					List<T2> pList_T2 = Value_T2.Cast<T2>().ToList();
					dictAttributesMulti.Add(pDerectType, (pList_T1, pList_T2));
				}
			}
		}

		public static void CollectAttributesMulti<T1, T2, T3>(Dictionary<Type, (List<T1>, List<T2>, List<T3>)> dictAttributesMulti)
			where T1 : Attribute
			where T2 : Attribute
			where T3 : Attribute
		{
			if (dictAttributesMulti == null)
			{
				throw new ArgumentNullException(nameof(dictAttributesMulti));
			}

			//
			Dictionary<Type, List<object>> dictAttributes_T1;
			Dictionary<Type, List<object>> dictAttributes_T2;
			Dictionary<Type, List<object>> dictAttributes_T3;
			Attributes.TryGetDecrectType(typeof(T1), out dictAttributes_T1);
			Attributes.TryGetDecrectType(typeof(T2), out dictAttributes_T2);
			Attributes.TryGetDecrectType(typeof(T3), out dictAttributes_T3);

			//
			if (dictAttributes_T1 == null || dictAttributes_T2 == null || dictAttributes_T3 == null)
			{
				return;
			}

			foreach (var item in dictAttributes_T1)
			{
				Type pDerectType = item.Key;
				if (isForget(pDerectType))
				{
					continue;
				}
				if (isCollectable<T1, T2, T3>(pDerectType))
				{
					continue;
				}
				if (item.Value.Count <= 0)
				{
					throw new ArgumentException("attribute is null");
				}
				if (dictAttributes_T2.TryGetValue(pDerectType, out var Value_T2))
				{
					if (dictAttributes_T3.TryGetValue(pDerectType, out var Value_T3))
					{
						List<T1> pList_T1 = item.Value.Cast<T1>().ToList();
						List<T2> pList_T2 = Value_T2.Cast<T2>().ToList();
						List<T3> pList_T3 = Value_T3.Cast<T3>().ToList();
						dictAttributesMulti.Add(pDerectType, (pList_T1, pList_T2, pList_T3));
					}
				}
			}
		}
	}
}


