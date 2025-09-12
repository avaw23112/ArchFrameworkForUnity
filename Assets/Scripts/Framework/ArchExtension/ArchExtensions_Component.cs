using Arch.Core;
using UnityEngine;

namespace Arch
{
	public interface IViewComponent : IComponent
	{
		public GameObject gameObject { get; set; }
	}

	public struct ViewComponent : IViewComponent
	{
		private GameObject m_gameObject;
		public GameObject gameObject
		{
			get { return m_gameObject; }
			set { m_gameObject = value; }
		}
	}

	public class ViewModleSyncSysmte : DestroySystem<ViewComponent>
	{
		protected override void Run(Entity entity, ref ViewComponent component_T1)
		{
			if (component_T1.gameObject != null)
			{
				Object.Destroy(component_T1.gameObject);
			}
		}
	}
}
