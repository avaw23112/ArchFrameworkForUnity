using Arch.Tools;
using Codes.Model.WeaverTest;
using Events;
using System.ComponentModel;

namespace Codes.Logic
{
	internal class WearverTest : Event<GameStartEvent>
	{
		public override void Run(GameStartEvent value)
		{
			Person person = new Person();
			person.PropertyChanged += OnViewModelPropertyChanged;
			person.FamilyName = "wearver success";
		}

		private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Person.FamilyName))
			{
				Person person = sender as Person;
				ArchLog.LogInfo(person.FamilyName);
			}
		}
	}
}