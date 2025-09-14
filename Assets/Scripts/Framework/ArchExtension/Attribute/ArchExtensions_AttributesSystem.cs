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
		public override void Process(UniqueAttribute attribute, Type derectType)
		{
			if (derectType.IsClass || derectType.IsAbstract)
			{
				Logger.Error($"{derectType} is not struct");
				throw new Exception($"{derectType} is not struct");
			}
			if (derectType.GetInterface(nameof(IComponent)) == null)
			{
				Logger.Error($"{derectType} is not component");
				throw new Exception($"{derectType} is not component");
			}
			MethodInfo setSingleMethod = typeof(SingletonComponent).GetMethod("SetSingle");
			MethodInfo genericMethod = setSingleMethod.MakeGenericMethod(derectType);
			genericMethod.Invoke(null, new object[] { Activator.CreateInstance(derectType) });
		}
	}

	#endregion

	#region 初始化World

	internal class WorldAttributeSystem : AttributeSystem<WorldAttribute>
	{
		public override void Process(WorldAttribute attribute, Type derectType)
		{
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