using Arch.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Arch
{
	public struct ViewComponent : IViewComponent
	{
		private GameObject m_gameObject;
		public GameObject gameObject
		{
			get { return m_gameObject; }
			set { m_gameObject = value; }
		}
	}

	[Unique]
	public struct EntityBindingComponent : IModelComponent
	{
		public Dictionary<Entity, List<Entity>> dicEntitiesBinding;
	}
}
