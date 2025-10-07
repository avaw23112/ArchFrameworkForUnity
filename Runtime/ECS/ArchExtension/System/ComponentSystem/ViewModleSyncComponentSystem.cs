using Arch.Core;
using UnityEngine;

namespace Arch
{
	[System]
	public class ViewModleSyncSystem : GlobalDestroySystem<ViewComponent>
	{
		protected override void Run(Entity entity, ref ViewComponent component_T1)
		{
			if (component_T1.gameObject != null)
			{
				GameObject.Destroy(component_T1.gameObject);
			}
		}
	}
}