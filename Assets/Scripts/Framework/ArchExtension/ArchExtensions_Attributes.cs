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

	public class UnitySystemAttribute : BaseAttribute
	{
		public string GroupName;

		public UnitySystemAttribute(string groupName)
		{
			GroupName = groupName;
		}

		public UnitySystemAttribute()
		{
			GroupName = "Default";
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