using Arch;
using Arch.Tools;
using Attributes;
using UnityEngine;

namespace Codes.Logic
{
	[Forget]
	[System]
	internal class TestSystem : IPureUpdate
	{
		public void Update()
		{
			ArchLog.LogDebug("99999");
		}
	}

	[System]
	internal class Test3System : IPureUpdate
	{
		private Transform footprintParent;
		private float lastSpawnTime;

		private void CreateFootprintSystem()
		{
			footprintParent = new GameObject("Footprints").transform;
		}

		public void Update()
		{
			if (footprintParent == null)
			{
				CreateFootprintSystem();
			}

			if (Time.time - lastSpawnTime > 0.2f)
			{
				var mark = GameObject.CreatePrimitive(PrimitiveType.Quad);
				mark.transform.SetParent(footprintParent);
				mark.transform.position = new Vector3(Random.Range(-2f, 2f), 0.01f, Random.Range(-2f, 2f));
				mark.transform.rotation = Quaternion.Euler(90, 0, 0);
				mark.GetComponent<Renderer>().material.color = new Color(1, 0.5f, 0, 0.7f);

				Object.Destroy(mark, 2f);
				lastSpawnTime = Time.time;
			}
		}
	}

	[Forget]
	[System]
	internal class Test1System : IPureAwake
	{
		public void Awake()
		{
			ArchLog.LogDebug("1");
		}
	}

	[Forget]
	[System]
	internal class Test2System : IPureAwake
	{
		public void Awake()
		{
			ArchLog.LogDebug("2");
		}
	}
}