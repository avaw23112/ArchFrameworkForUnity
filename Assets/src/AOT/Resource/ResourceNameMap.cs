using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arch
{
    /// <summary>
    /// 资源命名到 Address 映射（简化层级，统一管理资源）。
    /// 运行时部分：包含数据结构与查询逻辑。
    /// </summary>
    [CreateAssetMenu(fileName = "ResourceNameMap", menuName = "Arch/ResourceNameMap")]
    public partial class ResourceNameMap : ScriptableObject
    {
        #region 简化层级结构（可序列化）
        [SerializeField]
        [Tooltip("简化层级根集合（Addressables Group），一组对应一类资源")]
        protected List<ResourceRootGroup> _rootGroups = new List<ResourceRootGroup>();

        [Serializable]
        public class ResourceRootGroup
        {
            [Tooltip("根组名称（与 Addressables Group 对应）")]
            public string groupName;

            [Tooltip("一个根组下的子组（对应资源的一级路径分类）")]
            public List<ResourceSubGroup> subGroups = new List<ResourceSubGroup>();
        }

        [Serializable]
        public class ResourceSubGroup
        {
            [Tooltip("子组名称（资源一级路径，如 Prefabs、Icons 等）")]
            public string subGroupName;

            [Tooltip("子组内的具体资源（直接加载，层级已简化）")]
            public List<ResourceMapping> resources = new List<ResourceMapping>();
        }

        [Serializable]
        public class ResourceMapping
        {
            [Tooltip("资源名（全局唯一）")]
            public string resourceName;

            [Tooltip("Addressables 实际 Address")]
            public string address;

            [Tooltip("资源在项目的路径（用于调试/预览）")]
            public string assetPath;
        }
        #endregion

        #region 运行时查询字典
        [NonSerialized] private Dictionary<string, string> _nameToAddress;
        [NonSerialized] private bool _isInitialized;

        public Dictionary<string, string> NameToAddress
        {
            get
            {
                if (!_isInitialized) Initialize();
                return _nameToAddress;
            }
        }
        #endregion

        #region 运行时初始化（构建简化层级）
        public void Initialize()
        {
            if (_isInitialized && _nameToAddress != null) return;

            try
            {
                _nameToAddress = new Dictionary<string, string>();
                if (_rootGroups == null)
                {
                    _rootGroups = new List<ResourceRootGroup>();
                    Debug.LogWarning($"[{name}] 根组列表为空，已初始化为空列表");
                    return;
                }

                foreach (var rootGroup in _rootGroups)
                {
                    if (rootGroup?.subGroups == null) continue;

                    foreach (var subGroup in rootGroup.subGroups)
                    {
                        if (subGroup?.resources == null) continue;

                        foreach (var resource in subGroup.resources)
                        {
                            if (string.IsNullOrEmpty(resource?.resourceName) || string.IsNullOrEmpty(resource.address))
                            {
                                Debug.LogWarning($"[{name}] 无效资源：{rootGroup.groupName}/{subGroup.subGroupName}/{resource?.resourceName ?? "<空>"}");
                                continue;
                            }

                            if (_nameToAddress.ContainsKey(resource.resourceName))
                            {
                                Debug.LogWarning($"[{name}] 资源名重复，使用后者覆盖：{resource.resourceName}");
                                _nameToAddress[resource.resourceName] = resource.address;
                            }
                            else
                            {
                                _nameToAddress.Add(resource.resourceName, resource.address);
                            }
                        }
                    }
                }

                _isInitialized = true;
                Debug.Log($"[{name}] 初始化完成：共 {_nameToAddress.Count} 条有效映射");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{name}] 初始化失败：{e.Message}\n{e.StackTrace}");
                _isInitialized = false;
                _nameToAddress = new Dictionary<string, string>();
            }
        }
        #endregion

        #region 查询接口
        public bool TryGetAddress(string resourceName, out string address)
        {
            if (!_isInitialized) Initialize();
            if (string.IsNullOrEmpty(resourceName))
            {
                address = null;
                Debug.LogWarning($"[{name}] 资源名为空，查询失败");
                return false;
            }

            bool success = _nameToAddress.TryGetValue(resourceName, out address);
            if (!success) Debug.LogWarning($"[{name}] 未找到资源：{resourceName}");
            return success;
        }

        public List<ResourceMapping> GetAllMappings()
        {
            var allResources = new List<ResourceMapping>();
            if (_rootGroups == null) return allResources;

            foreach (var rootGroup in _rootGroups)
            {
                if (rootGroup?.subGroups == null) continue;
                foreach (var subGroup in rootGroup.subGroups)
                {
                    if (subGroup?.resources != null)
                        allResources.AddRange(subGroup.resources);
                }
            }
            return allResources;
        }
        #endregion

        #region 层级辅助（运行时与编辑器共享）
        private string GetFirstLevelPath(string fullSubPath)
        {
            if (string.IsNullOrEmpty(fullSubPath)) return string.Empty;
            string[] pathParts = fullSubPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            return pathParts.Length > 0 ? pathParts[0] : string.Empty;
        }

        private ResourceRootGroup GetOrCreateRootGroup(string groupName)
        {
            string safeName = string.IsNullOrEmpty(groupName) ? "<未命名Group>" : groupName;
            var existing = _rootGroups.Find(g => g.groupName == safeName);
            if (existing == null)
            {
                existing = new ResourceRootGroup { groupName = safeName };
                _rootGroups.Add(existing);
            }
            return existing;
        }

        private ResourceSubGroup GetOrCreateSubGroup(ResourceRootGroup rootGroup, string subGroupName)
        {
            if (rootGroup == null)
            {
                rootGroup = new ResourceRootGroup { groupName = "<临时Group>" };
                _rootGroups.Add(rootGroup);
            }

            if (rootGroup.subGroups == null)
                rootGroup.subGroups = new List<ResourceSubGroup>();

            var existing = rootGroup.subGroups.Find(sg => sg.subGroupName == subGroupName);
            if (existing == null)
            {
                existing = new ResourceSubGroup { subGroupName = subGroupName };
                rootGroup.subGroups.Add(existing);
            }
            return existing;
        }

        private void SortSimplifiedHierarchy()
        {
            _rootGroups?.Sort((a, b) => string.Compare(a.groupName, b.groupName, StringComparison.Ordinal));
            foreach (var rootGroup in _rootGroups ?? new List<ResourceRootGroup>())
            {
                rootGroup.subGroups?.Sort((a, b) => string.Compare(a.subGroupName, b.subGroupName, StringComparison.Ordinal));
                foreach (var subGroup in rootGroup.subGroups ?? new List<ResourceSubGroup>())
                {
                    subGroup.resources?.Sort((a, b) => string.Compare(a.resourceName, b.resourceName, StringComparison.Ordinal));
                }
            }
        }

        private void ClearHierarchy()
        {
            if (_rootGroups != null)
                _rootGroups.Clear();
            else
                _rootGroups = new List<ResourceRootGroup>();
        }

        private int GetTotalResourceCount()
        {
            int count = 0;
            foreach (var rootGroup in _rootGroups ?? new List<ResourceRootGroup>())
            {
                if (rootGroup?.subGroups == null) continue;
                foreach (var subGroup in rootGroup.subGroups)
                {
                    count += subGroup?.resources?.Count ?? 0;
                }
            }
            return count;
        }
        #endregion
    }
}

