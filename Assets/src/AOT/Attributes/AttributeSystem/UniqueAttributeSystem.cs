using Arch.Tools;
using Attributes;
using System;
using System.Reflection;

namespace Arch
{
	[NotHotReload]
	internal class UniqueAttributeSystem : AttributeSystem<UniqueAttribute>
	{
		public override void Process(UniqueAttribute attribute, Type directType)
		{
			if (directType.IsClass || directType.IsAbstract)
			{
				ArchLog.LogError($"{directType} is not struct");
				throw new Exception($"{directType} is not struct");
			}
			if (directType.GetInterface(nameof(IComponent)) == null)
			{
				ArchLog.LogError($"{directType} is not component");
				throw new Exception($"{directType} is not component");
			}
			// 通过反射调用泛型方法
			MethodInfo setSingleMethod = typeof(Unique)
				.GetMethod("Set", BindingFlags.Static | BindingFlags.Public);

			MethodInfo genericMethod = setSingleMethod.MakeGenericMethod(directType);

			// 通过反射创建参数实例（要求值类型有无参构造）
			object component = Activator.CreateInstance(directType);

			// 调用 SetSingle<T>(T value)
			genericMethod.Invoke(null, new[] { component });
		}
	}
}