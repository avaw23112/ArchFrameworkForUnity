using System;

namespace Attributes
{
    public class BaseAttribute : Attribute
    {
    }

    /// <summary>
    /// 作用：标记该属性为收集属性。
    /// <para> 当有两个以上集合属性共同标记时，如果不是用匹配的系统收集则无法收集成功。 </para>
    /// </summary>
    public class BaseCollectableAttribute : BaseAttribute
    {
    }
}