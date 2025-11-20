using Arch.Tools;
using Codes.Model.WeaverTest;
using Events;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Scripting;

namespace Codes.Logic
{
	[Preserve]
	internal class WearverTest : Event<GameStartEvent>
	{
		public override void Run(GameStartEvent value)
		{
			Person person = new Person();
			person.PropertyChanged += OnViewModelPropertyChanged;
			person.FamilyName = "wearver success";
		}

		private Transform footprintParent;
		private float lastSpawnTime;

		private void CreateFootprintSystem()
		{
			footprintParent = new GameObject("Footprints").transform;
		}

		private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Person.FamilyName))
			{
				Person person = sender as Person;
				ArchLog.LogInfo(person.FamilyName);
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
					mark.GetComponent<Renderer>().material.color = new Color(1, 1f, 1, 1f);

					Object.Destroy(mark, 2f);
					lastSpawnTime = Time.time;
				}
			}
		}
	}
}