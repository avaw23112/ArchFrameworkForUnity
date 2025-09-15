using Arch.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Attributes
{
    public class Assemblys : Singleton<Assemblys>
    {
        private const string ASSEMBLY_NAME = "Assembly-CSharp";

        private ConcurrentDictionary<string, Assembly> m_dicAssemblys;
        public static IEnumerable<Assembly> AllAssemblies => Instance.m_dicAssemblys.Values;

        public Assemblys()
        {
            m_dicAssemblys = new ConcurrentDictionary<string, Assembly>();
        }

        public static void LoadAssembliesParallel()
        {
            if (Instance.m_dicAssemblys == null)
            {
                throw new Exception("Assembly dictionary is not initialized!");
            }

            if (Instance.m_dicAssemblys.Count > 0) return;

            Assembly[] arrAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            Parallel.For(0, arrAssemblies.Length, i =>
            {
                var assembly = arrAssemblies[i];
                Instance.m_dicAssemblys.TryAdd(assembly.FullName, assembly);
            });
        }

        public static void LoadAssemblys()
        {
            if (Instance.m_dicAssemblys == null)
            {
                throw new Exception("程序集字典为空！");
            }
            if (Instance.m_dicAssemblys.Count > 0)
            {
                return;
            }
            Assembly[] arrAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < arrAssemblies.Length; i++)
            {
                Instance.m_dicAssemblys.TryAdd(arrAssemblies[i].FullName, arrAssemblies[i]);
            }
        }

        public static Assembly GetMainAssembly()
        {
            if (Instance.m_dicAssemblys == null)
            {
                throw new Exception("程序集字典为空！");
            }
            Assembly assembly = null;
            if (!Instance.m_dicAssemblys.TryGetValue(ASSEMBLY_NAME, out assembly))
            {
                throw new Exception("找不到主程序集，或未加载程序集！");
            }
            return assembly;
        }

        public static Assembly GetAssembly(string assemblyName)
        {
            if (Instance.m_dicAssemblys == null)
            {
                throw new Exception("程序集字典为空！");
            }
            Assembly assembly = null;
            if (!Instance.m_dicAssemblys.TryGetValue(assemblyName, out assembly))
            {
                throw new Exception("找不到目标程序集，或未加载程序集！");
            }
            return assembly;
        }

        public static Assembly GetCallingAssembly()
        {
            return Assembly.GetCallingAssembly();
        }

        public static Assembly GetEntryAssembly()
        {
            return Assembly.GetEntryAssembly();
        }

        public static Assembly GetExecutingAssembly()
        {
            return Assembly.GetExecutingAssembly();
        }
    }
}