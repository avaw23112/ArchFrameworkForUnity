using Attributes;
using System;
using Tools;

namespace Arch
{
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