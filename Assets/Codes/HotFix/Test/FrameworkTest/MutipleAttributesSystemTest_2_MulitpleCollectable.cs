using Attributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Test
{
    /// <summary>
    /// 测试多种属性的系统
    /// </summary>
    [MutipleAttributesSystemTest_1_MutipleAttribute]
    [MutipleAttributesSystemTest_1_MutipleAttribute]
    [MutipleAttributesSystemTest_1_MutipleAttribute]
    [MutipleAttributesSystemTest_1_MutipleAttribute]
    [MutipleAttributesSystemTest_1_MutipleAttribute]
    [MutipleAttributesSystemTest_2_MutipleAttribute]
    [MutipleAttributesSystemTest_2_MutipleAttribute]
    [MutipleAttributesSystemTest_2_MutipleAttribute]
    [Forget]
    public class MutipleAttributesSystemTest_2_MulitpleCollectable : MonoBehaviour
    {
        // Use this for initialization
        private void Start()
        {
            Attributes.Attributes.RegisterAttributeSystems();
        }

        // Update is called once per frame
        private void Update()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MutipleAttributesSystemTest_1_MutipleAttribute : BaseAttribute
    {
    }

    [Forget]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MutipleAttributesSystemTest_2_MutipleAttribute : BaseAttribute
    {
    }

    [Forget]
    public class MutipleAttributesSystemTest_1_MutipleAttributeSystem : AttributeSystem<MutipleAttributesSystemTest_1_MutipleAttribute>
    {
        /// <summary>
        /// 一次都不触发，因为它只负责处理单属性
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="derectType"></param>
        public override void Process(MutipleAttributesSystemTest_1_MutipleAttribute attribute, Type derectType)
        {
            Debug.Log("MutipleAttributesSystemTest_1_MutipleAttributeSystem.Process");
        }
    }

    [Forget]
    public class MutipleAttributesSystemTest_2_MutipleAttributeSystem : MutipleAttributeSystem<MutipleAttributesSystemTest_1_MutipleAttribute, MutipleAttributesSystemTest_2_MutipleAttribute>
    {
        /// <summary>
        /// 应当只触发一次
        /// </summary>
        /// <param name="derectType"></param>
        /// <param name="list_T1"></param>
        /// <param name="list_T2"></param>
        public override void Process(Type derectType, List<MutipleAttributesSystemTest_1_MutipleAttribute> list_T1, List<MutipleAttributesSystemTest_2_MutipleAttribute> list_T2)
        {
            Debug.Log("MutipleAttributesSystemTest_2_MutipleAttributeSystem.Process"
                + $"T1 count: {list_T1.Count}"
                + $"T2 count: {list_T2.Count}");
        }
    }
}