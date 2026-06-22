# Intel 大小核神经网络调度器 N 版 2.51 架构分析（基于反编译恢复）

本文描述的是 2.51 版本的“可从反编译直接观察到”的结构分层，用于指导恢复树拆分与后续继续逼近原作者工程结构。由于 2.51 缺失可用 PDB 直接证据（详见 `PDB_RECOVERY_NOTES.md`），本文侧重“运行时主线 + 代码层分层”，不做 compiland/sequence-point 级的文本校验承诺。

## 1. 结论摘要
- 2.51 仍然是以 `IntlThrdSchd.exe` 为主入口的 `net48` Windows 服务工程。
- 它继续覆盖：服务安装与驻留、ETW 采样、WinRing0/MSR 访问、线程特征观测、模型推理与线程亲和性控制。
- 模型侧核心仍是 Transformer 路线：存在 `TransformerScheduler`，并且在 2.51 中 `UpdateTAT` 接口包含 `energyValue`（推断用于把能耗/功耗信号引入在线更新）。
- 发布包中存在 `scheduler_model.bin.bak`，说明模型参数/权重很可能外置为二进制文件，并由服务在运行时加载或备份。
- 相比 2.36，2.51 新增了 `PROCESS_POWER_THROTTLING_STATE`，并通过 `SetProcessInformation(ProcessPowerThrottling=4, ...)` 调整进程电源节流状态，说明作者开始把 Windows 电源管理相关接口纳入调度控制链路。

## 2. 服务入口与安装链路
- 主运行链路：
  - `Program.Main()`
  - `ServiceBase.Run(new Service1())`
  - `Service1` 负责初始化与常驻循环
- 安装/卸载脚本：
  - `安装服务.bat`
  - `卸载服务.bat`
- 服务名仍使用 `IntlThrdPerfSchd`（与程序集/发布物 `IntlThrdSchd.exe` 命名并存，属于该项目一贯的“双词形”现象）。

## 3. 原生接口层（驱动/寄存器访问）
- `OpenLibSys.cs` 提供 `OpenLibSys.Ols` 封装，用于加载并调用：
  - `WinRing0.dll`
  - `WinRing0x64.dll`
- 这条链路说明该程序不仅仅是“设置 Windows 调度提示位”，而是会触达 CPU/MSR/驱动级接口来辅助调度决策与落核控制。

## 4. 观测与采样层（ETW）
- `Service1.cs` 中包含基于 `Microsoft.Diagnostics.Tracing.TraceEvent` 的 ETW 采样与进程/线程信息收集逻辑。
- 输入信号是实时观测与周期性统计，而非纯静态规则。

## 5. 调度与模型层（恢复树文件边界）
说明：下列文件边界是恢复树采用的“作者式组合文件”布局（用于贴近 2.36 已验证结构），不代表 2.51 编译时真实 `.cs` 文件切分。

### 5.1 `SchedulerModel.cs`
- 聚合点：调度核心状态与统计结构
- 关键类型：`CircularBuffer<T>`、`DecisionRecord`、`SchedulerStatistics`、`TransformerScheduler`
- 2.51 关注点：`TransformerScheduler.UpdateTAT(float currentTAT, float energyValue)`

### 5.2 `SchedulerService.cs`
- 调度外层封装与控制面：`SchedulerService`、`SchedulerController`

### 5.3 `TransformerLayers.cs`
- Transformer 组件与编码器：
  - `MathHelper`、`LinearLayer`、`LayerNormLayer`、`MultiHeadAttention`、`FeedForwardLayer`、`TransformerEncoderLayer`
  - `CoreTransformerEncoder`、`ThreadTransformerEncoder`

### 5.4 `OnlineLearning.cs` 与 `OnlineLearningManager.cs`
- `OnlineLearning.cs`：`MathCompat`、`GradientHelper`
- `OnlineLearningManager.cs`：在线学习/奖励窗口/训练与推理编排相关逻辑（以反编译为准）

## 6. 数学与 SIMD 支撑
- 关键文件：
  - `MatrixOperations.cs`
  - `VectorMath.cs`
  - `VectorMathNew.cs`
- 这些实现为模型推理和在线更新提供底层向量运算支持。

## 7. 线程控制层
- `Service1.cs` 中包含对线程亲和性的控制（如 `SetThreadIdealProcessor`、`SetThreadAffinityMask`），并围绕大小核集合、线程组等做落核决策。
- 因此 2.51 仍符合总判断：这是一个“主动控制线程落核”的调度系统，而非仅观测或仅建议。

## 8. 后续深挖方向（无 PDB 约束下）
- 优先从“可直接支撑文件边界/命名”的信号继续挖：资源名、日志文件名、配置键名、命令行参数、落盘文件等。
- 若后续能获得同版本或相近版本的 PDB，则可把 2.51 恢复树提升到 PDB/DIA 级别的可校验形态（compiland/sequence points/InjectedSource 校验）。
