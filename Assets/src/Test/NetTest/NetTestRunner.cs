using UnityEngine;
using Arch;
using Arch.Core;
using Arch.Core.Extensions;

namespace Arch.Net
{
    /// <summary>
    /// Net 测试运行器（场景组件）
    /// - 作用：在场景中快速搭建一个可运行的本地网络链路与测试数据，便于观察 Sync 扫描/应用全流程。
    /// - 用法：将该脚本挂到任意 GameObject 上，按需配置 Inspector 选项后进入 Play。
    /// </summary>
    public sealed class NetTestRunner : MonoBehaviour
    {
        [Header("传输配置")]
        [Tooltip("使用 LiteNetLib 回环服务器（会在场景内启动一个 Echo Server）")] 
        [SerializeField] private bool m_vUseLiteLoopback = false;

        [Tooltip("自定义 Endpoint（为空时自动根据 UseLite 决定）")] 
        [SerializeField] private string m_szEndpointOverride = string.Empty;

        [Header("测试数据")]
        [Tooltip("是否自动生成若干测试实体（含 TestPosition 与 NetworkOwner=本端）")] 
        [SerializeField] private bool m_vSpawnDemoEntities = true;

        [Tooltip("生成实体数量（用于块级扫描观察批量打包）")] 
        [SerializeField] private int m_nDemoEntityCount = 32;

        [Tooltip("是否启用块级扫描（否则使用逐实体扫描）")] 
        [SerializeField] private bool m_vUseChunkScan = true;

        [Header("所有权")] 
        [Tooltip("本端 ClientId（用于过滤 Owned 实体）")] 
        [SerializeField] private int m_nMyClientId = 1;

        private LiteNetLibEchoServer m_pEcho;

        private void Awake()
        {
            OwnershipService.MyClientId = m_nMyClientId;
            NetworkSettings.Config.GetType(); // 触发设置对象加载

            // 配置扫描模式
            typeof(NetworkConfig).GetField("m_vUseChunkScan", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(NetworkSettings.Config, m_vUseChunkScan);

            // 端点与本地 EchoServer
            string endpoint = m_szEndpointOverride;
            if (string.IsNullOrEmpty(endpoint))
            {
                if (m_vUseLiteLoopback)
                {
                    endpoint = "lite://127.0.0.1:9050";
                    var go = new GameObject("LiteNetLibEchoServer");
                    m_pEcho = go.AddComponent<LiteNetLibEchoServer>();
                }
                else
                {
                    endpoint = "loopback://local";
                }
            }

            // 建立网络运行时，触发 NetworkAwakeSystem & NetworkUpdateSystem
            SingletonComponent.Set(new NetworkRuntime { Endpoint = endpoint });

            // 构造演示实体（Owned）
            if (m_vSpawnDemoEntities)
            {
                var w = NamedWorld.DefaultWord;
                for (int i = 0; i < m_nDemoEntityCount; i++)
                {
                    var e = w.Create(new Assets.Scripts.Test.Net.TestPosition { x = i, y = 0, z = 0 });
                    e.Add<NetworkOwner>();
                    e.Setter((ref NetworkOwner no) => no.OwnerClientId = m_nMyClientId);
                }
            }
        }

        private void OnDestroy()
        {
            if (m_pEcho != null)
            {
                Destroy(m_pEcho.gameObject);
                m_pEcho = null;
            }
        }
    }
}

