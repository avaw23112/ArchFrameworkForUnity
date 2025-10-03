# UnitFactory 使用说明（参考 ET 的 Unit 设计）

- 创建本地 Unit（非强制网络）
  ```csharp
  var u = Arch.Net.UnitFactory.CreateUnit(Arch.NamedWorld.DefaultWord,;
                                          });
  ```

- 创建网络 Unit（自动补全 NetworkOwner/NetworkEntityId，并对齐 Unit.UnitId）
  ```csharp
  var uNet = Arch.Net.UnitFactory.CreateNetworkUnit(Arch.NamedWorld.DefaultWord,;
  ```

- 升级已有实体为 Unit / 网络 Unit
  ```csharp
  var e = Arch.NamedWorld.DefaultWord.Create();
  Arch.Net.UnitFactory.EnsureAsUnit(ref e, networked: true);
  ```

- 全局 Hook（用于统一挂默认组件、记录统计或绑定表现对象）
  ```csharp
  Arch.Net.UnitFactory.GlobalInitHook = ent => {
      // 例如：默认挂某些组件、记录创建日志、或绑定 GameObject 表现
  };
  ```

