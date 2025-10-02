# Net 测试框架（Assets/Net/NetTest）

用途：本目录提供最小可用的本地网络与同步链路测试环境，便于在 Unity Play 模式下观察 Sync 扫描/应用的行为、验证所有权筛选与分段/Delta 等路径。

快速使用：

- 将 `NetTestRunner` 组件挂到任意场景中的 GameObject 上；
- 在 Inspector 中选择：
  - 是否启用 `Use Lite Loopback`（启用后会在场景内启动一个 `LiteNetLibEchoServer`，并使用 `lite://127.0.0.1:9050` 作为 Endpoint；否则使用内存回环 `loopback://local`）；
  - `Use Chunk Scan`（控制使用块级扫描或逐实体扫描）；
  - `Spawn Demo Entities` 与数量（用于快速观察批量同步与 Delta）；
  - `My Client Id`（用于过滤仅上发本端拥有的实体）。

说明：

- 进入 Play 后，`NetworkAwakeSystem` 会自动初始化 `NetworkSingleton.Session` 并注册消息处理与路由策略；
- `NetworkUpdateSystem` 每帧驱动传输轮询与命令/数据队列；
- 同步扫描：根据 `NetworkSettings.Config.UseChunkScan` 选择 `SyncChunkScanSystem` 或 `SyncScanSystem`；
- 同步应用：`SyncApplySystem` 从 `SyncIncomingQueue` 中取包并应用到世界；
- `NetTest` 目录中的其它示例（如 `TestPosition*`）提供了最简单的同步组件与移动逻辑。

注意：

- 若在 IL2CPP/HybridCLR 环境使用，需要确保泛型 AOT 桩已收集（例如 `ComponentApplierRegistry` / `ComponentPackerRegistry` 相关泛型）。
- 若需要更细粒度的自动化断言，可在项目中引入 Unity Test Framework，并基于此目录中的组件/系统编写 PlayMode 测试用例。

