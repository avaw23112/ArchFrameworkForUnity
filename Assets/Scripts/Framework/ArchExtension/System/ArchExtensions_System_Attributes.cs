using Attributes;
using System;
using System.Reflection;
using Tools;

namespace Arch
{
	internal static class EntitasAttributesExtensionsHelper
	{
		public static bool isMarkedSystem(Type derectType)
		{
			return derectType.GetCustomAttributes(typeof(SystemAttribute), false).Length > 0;
		}
	}

	#region 单例组件初始化

	internal class UniqueAttributeSystem : AttributeSystem<UniqueAttribute>
	{
		public override void Process(UniqueAttribute attribute, Type directType)
		{
			if (directType.IsClass || directType.IsAbstract)
			{
				Logger.Error($"{directType} is not struct");
				throw new Exception($"{directType} is not struct");
			}
			if (directType.GetInterface(nameof(IComponent)) == null)
			{
				Logger.Error($"{directType} is not component");
				throw new Exception($"{directType} is not component");
			}
			// 通过反射调用泛型方法
			MethodInfo setSingleMethod = typeof(SingletonComponent)
				.GetMethod("Set", BindingFlags.Static | BindingFlags.Public);

			MethodInfo genericMethod = setSingleMethod.MakeGenericMethod(directType);

			// 通过反射创建参数实例（要求值类型有无参构造）
			object component = Activator.CreateInstance(directType);

			// 调用 SetSingle<T>(T value)
			genericMethod.Invoke(null, new[] { component });
		}
	}

	#endregion

	#region 初始化World

	internal class WorldAttributeSystem : AttributeSystem<WorldAttribute>
	{
		public override void Process(WorldAttribute attribute, Type derectType)
		{
			if (typeof(IGlobalSystem).IsAssignableFrom(derectType))
			{
				Logger.Error($"系统：{derectType} 属于全局系统，不应该被World属性标记！");
				throw new Exception($"系统：{derectType} 属于全局系统，不应该被World属性标记！");
			}
			if (typeof(ISystem).IsAssignableFrom(derectType))
			{
				Logger.Error($"类型：{derectType} 非可设置世界的系统，请检查实现！");
				return;
			}
			if (!typeof(IReactiveSystem).IsAssignableFrom(derectType))
			{
				Logger.Error($"类型：{derectType} 非系统，请检查实现！");
				throw new Exception($"类型：{derectType} 非系统，请检查实现！");
			}
			NamedWorld.CreateNamed(attribute.worldName);
		}
	}

	#endregion 初始化World

	#region 用于对After、Before、AfterAndBefore属性的正确性校验

	internal class AfterAttributeSystem : AttributeSystem<AfterAttribute>
	{
		public override void Process(AfterAttribute attribute, Type derectType)
		{
			if (!EntitasAttributesExtensionsHelper.isMarkedSystem(derectType))
			{
				Logger.Error($"在{derectType.Name}中，After属性必须和System属性一起使用！ ");
				throw new Exception($"在{derectType.Name}中，After属性必须和System属性一起使用！");
			}
		}
	}

	internal class BeforeAttributeSystem : AttributeSystem<BeforeAttribute>
	{
		public override void Process(BeforeAttribute attribute, Type derectType)
		{
			if (!EntitasAttributesExtensionsHelper.isMarkedSystem(derectType))
			{
				Logger.Error($"在{derectType.Name}中，Before属性必须和System属性一起使用！ ");
				throw new Exception($"在{derectType.Name}中，Before属性必须和System属性一起使用！");
			}
		}
	}

	internal class BeforeAndAfterAttributeSystem : AttributeSystem<AfterAttribute, BeforeAttribute>
	{
		public override void Process(AfterAttribute attribute_T1, BeforeAttribute attribute_T2, Type derectType)
		{
			if (!EntitasAttributesExtensionsHelper.isMarkedSystem(derectType))
			{
				Logger.Error($"在{derectType.Name}中，After和Before属性必须和System属性一起使用！ ");
				throw new Exception($"在{derectType.Name}中，After和Before属性必须和System属性一起使用！");
			}
		}
	}

	#endregion 用于对After、Before、AfterAndBefore属性的正确性校验
}