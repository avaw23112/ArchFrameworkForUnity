using Attributes;
using System;
using Tools;

namespace Arch
{
    internal class FirstAttributeSystem : AttributeSystem<FirstAttribute>
    {
        public override void Process(FirstAttribute attribute, Type derectType)
        {
            if (!AttributeSystemHelper.isMarkedSystem(derectType))
            {
                Logger.Error($"在{derectType.Name}中，First属性必须和System属性一起使用！ ");
            }

            if ((derectType.GetCustomAttributes(typeof(AfterAttribute), false).Length > 0) || derectType.GetCustomAttributes(typeof(BeforeAttribute), false).Length > 0)
            {
                Logger.Error($"在{derectType.Name}中，First属性不能和After和Before属性一起使用！ ");
            }
        }

        internal class LastAttributeSystem : AttributeSystem<LastAttribute>
        {
            public override void Process(LastAttribute attribute, Type derectType)
            {
                if (!AttributeSystemHelper.isMarkedSystem(derectType))
                {
                    Logger.Error($"在{derectType.Name}中，Last属性必须和System属性一起使用！ ");
                }
                if ((derectType.GetCustomAttributes(typeof(AfterAttribute), false).Length > 0) || derectType.GetCustomAttributes(typeof(BeforeAttribute), false).Length > 0)
                {
                    Logger.Error($"在{derectType.Name}中，Last属性不能和After和Before属性一起使用！ ");
                }
            }
        }

        internal class AfterAttributeSystem : AttributeSystem<AfterAttribute>
        {
            public override void Process(AfterAttribute attribute, Type derectType)
            {
                if (!AttributeSystemHelper.isMarkedSystem(derectType))
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
                if (!AttributeSystemHelper.isMarkedSystem(derectType))
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
                if (!AttributeSystemHelper.isMarkedSystem(derectType))
                {
                    Logger.Error($"在{derectType.Name}中，After和Before属性必须和System属性一起使用！ ");
                    throw new Exception($"在{derectType.Name}中，After和Before属性必须和System属性一起使用！");
                }
            }
        }
    }
}