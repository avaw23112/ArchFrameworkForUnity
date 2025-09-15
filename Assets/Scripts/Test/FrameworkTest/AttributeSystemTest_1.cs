using Attributes;
using System;
using UnityEngine;

[AttributeSystemTest_1Attribute(nameof(AttributeSystemTest_1AttributeSystem))]
[Forget]
public class AttributeSystemTest_1 : MonoBehaviour
{
    // Start is called before the first frame update
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
public class AttributeSystemTest_1AttributeSystem : AttributeSystem<AttributeSystemTest_1Attribute>
{
    public AttributeSystemTest_1AttributeSystem()
    {
    }

    public override void Process(AttributeSystemTest_1Attribute attribute, Type derectType)
    {
        Arch.Tools.ArchLog.Debug("AttributeSystemTest_1AttributeSystem.Process: " + attribute.name);
        if (derectType == typeof(AttributeSystemTest_1))
        {
            Arch.Tools.ArchLog.Debug("AttributeSystemTest_1AttributeSystem.Process: " + attribute.name + " is applied to " + derectType.Name);
        }
    }
}

[Forget]
public class AttributeSystemTest_1Attribute : BaseAttribute
{
    public string name;

    public AttributeSystemTest_1Attribute(string name)
    {
        this.name = name;
    }
}