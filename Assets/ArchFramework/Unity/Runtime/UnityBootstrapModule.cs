using Arch.Resource;
using Arch.Tools;
using Assets.src.AOT.ECS.SystemScheduler;

namespace Arch.DI
{
	public class UnityBootstrapModule : IService
	{
		public void ConfigureServices(IServiceCollection services)
		{
			//日志
			services.AddSingleton<IArchLogger>(sp => new UnityLogger());
			// 资源
			services.AddSingleton<IResProvider>(sp => new UnityResProvider());
			// 程序集加载
			services.AddSingleton<IAssemblyLoader>(sp => new UnityAssemblyLoader());
			// 系统调度器
			services.AddSingleton<ISystemScheduler>(sp => new UnityPlayerLoopScheduler());
			// 系统排序器
			services.AddSingleton<ISystemSorter>(sp => new UnitySystemSorter());
			// 加载界面
			services.AddSingleton<ILoadProgress>(sp => new UnityLoadProgress());
		}
	}
}