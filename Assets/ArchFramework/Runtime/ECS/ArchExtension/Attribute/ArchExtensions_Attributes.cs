using Attributes;
using System;

namespace Arch
{
	/// <summary>
	/// 标记System运转在哪个World
	/// </summary>
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

	/// <summary>
	/// 在开局时自动注册被标记的组件到单例世界中，且维护它的Get
	/// </summary>
	[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false)]
	public class UniqueAttribute : BaseAttribute
	{
	}

	public class SystemAttribute : BaseAttribute
	{
		public SystemAttribute()
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

	public class LastAttribute : BaseAttribute
	{
	}

	public class FirstAttribute : BaseAttribute
	{
	}
}