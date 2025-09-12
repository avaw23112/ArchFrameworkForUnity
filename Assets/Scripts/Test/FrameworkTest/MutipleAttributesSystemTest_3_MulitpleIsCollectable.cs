using Attributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Test
{
    [Forget]
    [MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes_2]
    [MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes_2]
    [MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes_2]
    [MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes_2]
    [MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes]
    [MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes]
    [MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes]
    public class MutipleAttributesSystemTest_3_MulitpleIsCollectable : MonoBehaviour
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

    [Forget]
    public class MutipleAttributesSystemTest_5_MulitpleIsCollectable_System : MutipleAttributeSystem<MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes,
            MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes_2>
    {
        public override void Process(Type derectType, List<MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes> list_T1, List<MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes_2> list_T2)
        {
            Debug.Log("MutipleAttributesSystemTest_5_MulitpleIsCollectable_System.Process");
        }
    }

    [Forget]
    public class MutipleAttributesSystemTest_3_MulitpleIsCollectable_System : MutipleAttributeSystem<MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes>
    {
        public override void Process(Type derectType, List<MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes> list_T)
        {
            Debug.Log("MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes.Process");
        }
    }

    [Forget]
    public class MutipleAttributesSystemTest_4_MulitpleIsCollectable_System : MutipleAttributeSystem<MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes_2>
    {
        public override void Process(Type derectType, List<MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes_2> list_T)
        {
            Debug.Log("MutipleAttributesSystemTest_4_MulitpleIsCollectable_System.Process");
        }
    }

    [Forget]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes_2 : BaseCollectableAttribute
    {
        public MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes_2()
        {
        }
    }

    [Forget]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes : BaseCollectableAttribute
    {
        public MutipleAttributesSystemTest_3_MulitpleIsCollectable_Attributes()
        {
        }
    }
}