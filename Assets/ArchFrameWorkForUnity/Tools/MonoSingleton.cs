using UnityEngine;

namespace Arch.Tools
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T m_instance;

        public static T Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = FindFirstObjectByType<T>();
                    if (m_instance == null)
                    {
                        GameObject obj = new GameObject(typeof(T).Name);
                        m_instance = obj.AddComponent<T>();
                    }
                }
                return m_instance;
            }
        }

        public void Init()
        {
        }

        protected void Start()
        {
            DontDestroyOnLoad(m_instance);
            OnStart();
        }

        protected virtual void OnStart()
        {
        }
    }
}