using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.src.AOT.ECS.SystemScheduler
{
	public interface ISystemScheduler
	{
		void Start(Action update, Action lateUpdate);

		void Stop();
	}
}