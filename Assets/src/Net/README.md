# Arch.Net 使用指南与技术文档（Unity）

本文分为两大部分：
- 使用指南：面向新上手开发者，快速完成接入、配置与验证。
- 技术文档：面向想了解原理与实现细节的开发者，说明技术选型、设计模式、优势/劣势，以及 C# 性能技巧。

---

## 一、使用指南

### 1. 前置条件
- Unity 2021+（Mono 或 IL2CPP）。
- 已包含 Arch.Core 与本项目 AOT 帮助代码（Assets/AOT）。
- 可选：LiteNetLib（UDP 传输）。

### 2. 快速接入
- 启动与会话初始化（GameRoot.cs 已内置）
  - 读取环境变量 `ARCH_CLIENT_ID` 作为本端 ClientId（默认 1），写入 `OwnershipService.MyClientId`。
  - 从 `NetworkSettings.Config.DefaultEndpoint` 读取 Endpoint，回退 `loopback://local`。
  - 写入 `SingletonComponent.Set(new NetworkRuntime { Endpoint = endpoint })` 触发网络系统初始化。

- 常用配置（Assets/Net/Config/NetworkConfig.cs）
  - `DefaultEndpoint`：`loopback://local` 或 `lite://ip:port`。
  - `UseChunkScan`：是否使用“块级 memcpy 扫描”（true）或“逐实体扫描”（false）。
  - `EntitiesPerPacket`、`PacketsPerFrame`：发送节流参数。
  - `EnableCompression`、`CompressThresholdBytes`：大包压缩阈值。
  - `EnableSyncRelay` 及 TTL/去重窗口：是否启用同步中继与相关参数。

- 创建网络实体（推荐入口）
  - NetworkEntityFactory（集中创建与元数据补全）
    - `CreateUnit(World, Action<Entity> configure=null)`：本地 Unit；在 configure 中添加名称/位置等组件。
    - `CreateNetworkUnit(World, Action<Entity> configure=null)`：网络 Unit；自动补全 NetworkOwner/NetworkEntityId，并将 `Unit.UnitId` 对齐为网络实体 Id。
  - GameUnits（常见类型一键创建）
    - `CreatePlayer(world, x, y, z)`、`CreateNpc(world, networked, x, y, z)`、`CreateSpectator(world)`、`PromoteToPlayer(ref entity)`。
  - UnitBuilder（链式 API）
    - `UnitBuilder.Begin(world).Networked().WithName("Player").With(new TestPosition{...}).Build();`
  - 名称/阵营/逻辑数据都应以组件方式提供（如 `UnitName`、`TestPosition`）。

- 编写可同步组件
  - 为“值类型组件”添加 `[NetworkSync]` 参与同步。
  - 经常小幅变化的数据（如位置/速度）可再加 `[SyncDelta]`，在“块级扫描”路径中启用 delta 编码降低带宽。

- 运行与测试
  - 直接 Play：`GameRoot` 会初始化网络会话、注册系统并驱动更新。
  - PlayMode 用例：`Assets/Net/NetTest/PlayMode/BuildAndSend_PlayModeTests.cs` 验证逐实体 BuildAndSend → loopback 收到 Sync 包。
  - 可选场景组件：`NetTestRunner` 和 `LiteNetLibEchoServer`（本地回环 UDP）。

### 3. 常见问题（FAQ）
- 未收到同步包：
  - 查看启动日志中的 `[NetInit] ClientId=... Endpoint=...`，确认 Endpoint 与 Relay 开关。
- IL2CPP/HybridCLR AOT：
  - 框架在启动时完成泛型闭包（发送/应用委托）；发布时避免剥离相关程序集；必要时将关键泛型加入 AOT 列表。
- Delta 效果异常：
  - Delta 基线与接收端实体对齐有关：请先在“无 delta”情况下验证原始包是否正确，再开启 `[SyncDelta]` 对比包长。

---

## 二、技术文档

### 1. 模块结构总览
- 传输（Transport）：`MockLoopbackTransport`（进程内回环）、`LiteNetLibTransport`（UDP）。
- 会话（Session）：`Session` 负责事件、数据入队、RPC 注册与分发。
- 协议（Protocol）：
  - `PacketHeader`（统一头部，包含版本/类型/定位/标志位等）。
  - `PacketBuilder`（构包）：支持“段数组重载”与“回调重载（FillDelegate）”。
- 同步（Sync）：
  - 发送：逐实体 `SyncScanSystem`（BuildAndSend）与块级 `SyncChunkScanSystem`（回调构包 + memcpy/delta）。
  - 接收：`SyncApplySystem` 解析段并写回世界，优先 Chunk 快路径、失败回退强类型应用器。
  - Delta：`SenderDeltaCache`/`ReceiverDeltaCache`（掩码 + 差异字节）。
  - Chunk 访问：`WorldChunkAccessor` + `ChunkAccess`（零拷贝指针、elemSize、stride）。
- 路由/中继：`SyncRelayService`、`NetworkRouter`、拓扑图与去重窗。
- 注册表：`SyncTypeCache`、`ArchetypeRegistry`。

### 2. 技术选型
- ECS + Chunk SoA：数据局部性好，适合 memcpy；类型稳定便于段定义。
- Unsafe/Span/stackalloc：缩减拷贝与分配；回调构包直接写入最终 payload。
- ArrayPool<byte>：租借大缓冲；用后立即归还，禁止跨帧持有。
- VarInt + 小端序：长度紧凑、跨平台一致。
- 反射只在启动期：绑定强类型委托，运行时逻辑零反射。

### 3. 设计模式
- 委托注册表：`SyncTypeCache` 一次性收集 `[NetworkSync]` 值类型，绑定 `BuildAndSendGeneric<T>`。
- 回调构包：`FillDelegate(int index, Span<byte> dst)` 直接写 payload，避免“段数组中间体”。
- 工厂/Builder：`NetworkEntityFactory`、`UnitFactory`、`UnitBuilder` 集中构建实体，统一扩展点。

### 4. 设计优劣势
- 优势：
  - 热路径零反射、低 GC；大批量 memcpy；delta 降低带宽；模块化可选（传输/路由/压缩）。
  - 两种同步路径（逐实体/块级）可按场景切换。
- 劣势：
  - Unsafe 使用需要边界与生命周期管理（及时释放 chunk buffer）。
  - IL2CPP 泛型闭包/裁剪需留意（启动期绑定、避免剥离）。
  - Delta 依赖接收端实体对齐与基线一致性（需要严格的映射策略）。

### 5. C# 性能要点（降 GC 与高效内存）
- 两段式：先计数再一次性分配/租借精确大小的缓冲（避免 List<T> 中转）。
- ArrayPool<byte>：大段数据统一租借，构包后归还；切勿缓存到成员或跨帧持有。
- stackalloc：小数据（如 ≤4KB）在栈上分配，零 GC。
- Span/ReadOnlySpan：切片视图，不复制；与 `Unsafe.CopyBlockUnaligned` 搭配高效。
- 避免闭包/捕获：热点循环中使用局部变量、for 循环，减少委托捕获分配。
- 小函数内联：对 varint/unaligned 等微函数使用 AggressiveInlining（慎用，实际测试为准）。

### 6. Unsafe 的作用与约束
- 非对齐读写：`Unsafe.WriteUnaligned`/`ReadUnaligned` 将 struct ↔ bytes 直接转换，避免逐字段序列化。
- 快速拷贝：`Unsafe.CopyBlockUnaligned` 在 chunk buffer → payload 做一次性拷贝。
- 约束：
  - 始终基于已计算的长度/下标操作 `Span<byte>`，避免越界。
  - 获取的 chunk 指针生命周期短，立即 `ReleaseComponentBuffer`；禁止跨作用域持有。

### 7. 内存布局与同步流程
- Archetype/Chunk 连续存储；通过 `elemSize` 与 `stride` 计算偏移。
- 段（Segment）由 meta 描述：包含 `typeId/elemSize/flags/length`。
- 回调构包：根据 meta 为每段分配写入窗口，直接 memcpy 或 delta 直写。

### 8. Delta 编码
- 预估阶段：`GetDeltaEncodedLengthRaw` 比较 baseline 与当前批次，决定 raw/delta 哪个更短。
- 写入阶段：`WriteDeltaToSpanRaw` 写入 `[maskLen varint][mask][delta]`，并更新 baseline。
- 接收端：`ReceiverDeltaCache.ApplyDelta` 以 baseline + mask/delta 还原 raw，再写入世界。

### 9. 可扩展性
- 新组件：添加 `[NetworkSync]`；若为“微变更”组件可加 `[SyncDelta]`。
- 单元构建：在 `UnitFactory`/`UnitBuilder`/`GameUnits` 中集中添加组件，便于统一优化或接入对象池/统计。
- 路由/压缩：切换选择器、实现自定义压缩器 `ICompressor`。

### 10. IL2CPP/AOT 注意事项
- 启动期进行泛型闭包与委托绑定，运行期避免反射。
- 避免裁剪：保留包含关键泛型的程序集；必要时手工加入 AOT 引用。

### 11. 验证与 Profiling 建议
- Profiler 观察项：
  - GC Alloc（发送/构包/接收）、Job/主线程耗时、发送包大小。
  - 比较逐实体 vs 块级、raw vs delta 的性能差异。
- 日志：
  - 启动日志 `[NetInit]`、`SyncApply`/Relay 采样日志（可在配置中开启采样率）。

---

如需进一步的“调优清单”（移动端参数建议、Profiler 快速检查项）或“常见陷阱”文档，我可以在此 ReadMe 的基础上继续补充。
