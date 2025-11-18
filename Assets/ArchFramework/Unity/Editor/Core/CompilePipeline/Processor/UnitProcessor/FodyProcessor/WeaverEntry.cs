using System;

namespace Arch.Compilation.Editor
{
	internal class WeaverEntry
	{
		public string AssemblyName;
		public string AssemblyPath;
		public string Element;
		public string TypeName;
		public Type WeaverType;

		public object WeaverInstance;

		public string PrettyName()
		{
			if (WeaverType == null)
				return "invalid weaver: " + AssemblyName + "::" + TypeName;
			return WeaverType.Assembly.GetName().Name + "::" + WeaverType.FullName;
		}

		internal void SetProperty(string property, object value)
		{
			WeaverType.GetProperty(property).SetValue(WeaverInstance, value, null);
		}

		internal void TrySetProperty(string property, object value)
		{
			var prop = WeaverType.GetProperty(property);
			if (prop == null) return;
			prop.SetValue(WeaverInstance, value, null);
		}

		internal void TryAddEvent(string evt, Delegate value)
		{
			var ev = WeaverType.GetEvent(evt);
			if (ev == null) return;
			ev.AddEventHandler(WeaverInstance, value);
		}

		internal void Activate(Type weaverType)
		{
			WeaverType = weaverType;
			WeaverInstance = Activator.CreateInstance(weaverType);
		}

		internal void Run(string methodName)
		{
			var method = WeaverType.GetMethod(methodName);
			if (method == null)
				throw new MethodAccessException("Could not find a public method named " + methodName + " in the type " + WeaverType);
			method.Invoke(WeaverInstance, null);
		}
	}
}