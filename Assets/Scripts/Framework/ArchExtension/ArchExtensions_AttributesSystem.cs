using Attributes;
using System;
using Tools;

namespace Arch
{
	internal static class EntitasAttributesExtensionsHelper
	{
		public static bool isMarkedSystem(Type derectType)
		{
			return derectType.GetCustomAttributes(typeof(UnitySystemAttribute), false).Length > 0;
		}
	}
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