using Arch.Core;
using Attributes;
using System;

namespace Arch
{
	public class WorldAttribute : BaseAttribute
	{
		public string worldName;
		public WorldAttribute(string worldName)
		{
			this.worldName = worldName;
		}
		public WorldAttribute()
		{
			worldName = "Default";
		}
	}
	[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
	public class UniqueAttribute : BaseAttribute
	{
		//封装Create方法，禁止通过Create创建标记为Unique的组件
		//开设UniqueComponentSetter<>,UniqueComponentGetter<>，GetUniqueComponent,SetUniqueComponent。操作单例组件
	}
	public class UnitySystemAttribute : BaseAttribute
	{
		public UnitySystemAttribute()
		{
		}
	}

	public class AfterAttribute : BaseCollectableAttribute
	{
		public Type At;

		public AfterAttribute(Type afterAt)
		{
			At = afterAt;
		}
	}

	public class BeforeAttribute : BaseCollectableAttribute
	{
		public Type At;

		public BeforeAttribute(Type beforeAt)
		{
			At = beforeAt;
		}
	}
}