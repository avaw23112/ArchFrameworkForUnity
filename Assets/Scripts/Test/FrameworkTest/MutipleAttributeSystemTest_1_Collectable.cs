using Arch;
using Attributes;
using System;
using UnityEngine;

namespace Assets.Scripts.Test
{
    /// <summary>
    /// 测试能否成功收集
    /// </summary>
    public class MutipleAttributeSystemTest_1_Collectable : MonoBehaviour
    {
        private void Start()
        {
            Attributes.Attributes.RegisterAttributeSystems();
        }

        private void Update()
        {
        }
    }

    //[System]
    //public class MutipleAttributeSystemTest_1_System
    //{
    //}

    //[System]
    //[Before(typeof(MutipleAttributeSystemTest_2_System))]
    //public class MutipleAttributeSystemTest_2_System
    //{
    //}

    //[System]
    //[After(typeof(MutipleAttributeSystemTest_3_System))]
    //public class MutipleAttributeSystemTest_3_System
    //{
    //}

    //[System]
    //[After(typeof(MutipleAttributeSystemTest_4_System))]
    //[Before(typeof(MutipleAttributeSystemTest_4_System))]
    //public class MutipleAttributeSystemTest_4_System
    //{
    //}

    //[System]
    //[After(typeof(MutipleAttributeSystemTest_5_System))]
    //[Before(typeof(MutipleAttributeSystemTest_5_System))]
    //public class MutipleAttributeSystemTest_5_System
    //{
    //}
    [Forget]
    public class MutipleAttributeSystemTest_3_CollectableAttribute : AttributeSystem<SystemAttribute, AfterAttribute, BeforeAttribute>
    {
        private int renferCount = 0;

        public override void Process(SystemAttribute attribute_T1, AfterAttribute attribute_T2, BeforeAttribute beforeAtAttribute, Type derectType)
        {
            Debug.Log($"MutipleAttributeSystemTest_3_CollectableAttribute : ref count: {renferCount++}");
        }
    }

    [Forget]
    public class MutipleAttributeSystemTest_2_CollectableAttribute : AttributeSystem<SystemAttribute, AfterAttribute>
    {
        private int renferCount = 0;

        public override void Process(SystemAttribute attribute_T1, AfterAttribute attribute_T2, Type derectType)
        {
            Debug.Log($" MutipleAttributeSystemTest_2_CollectableAttribute : ref count: {renferCount++}");
        }
    }

    [Forget]
    public class MutipleAttributeSystemTest_1_CollectableAttribute : AttributeSystem<SystemAttribute, BeforeAttribute>
    {
        private int renferCount = 0;

        public override void Process(SystemAttribute attribute_T1, BeforeAttribute attribute_T2, Type derectType)
        {
            Debug.Log($"MutipleAttributeSystemTest_1_CollectableAttribute :ref count: {renferCount++}");
        }
    }
}