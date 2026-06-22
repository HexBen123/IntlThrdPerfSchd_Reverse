# Intel 大小核神经网络调度器 N 版 2.35 架构分析

## 1. 结论摘要

2.35 当前最可信的结构判断是：

- 这是一个以 `IntlThrdSchd.exe` 为主入口的 `net48` Windows 服务工程。
- 与 2.34 发布包相比，2.35 更像已经把部分原先分散在 `CoreScheduler.dll` / `SimdLibrary.dll` 的类型重新并回单一 exe 主工程。
- 它不是只改 Windows 调度提示位的小工具，而是一个同时覆盖服务安装、ETW 采样、MSR/CPUID 访问、线程特征分析、神经网络推理和线程亲和性控制的完整调度系统。

## 2. 服务入口与安装链路

从恢复后的源码和安装脚本可以确认，运行主线是：

- `Program.Main()`
- `ServiceBase.Run(new Service1())`
- `Service1` 负责初始化和长期驻留

安装层同时存在两条证据链：

- 脚本层
  - `install-service.ps1`
  - `安装服务.bat`
- 工程层
  - `ProjectInstaller.cs`
  - `ProjectInstaller.Designer.cs`

其中 `ProjectInstaller` 明确把服务名、显示名和自动启动行为绑定到 `IntlThrdSchd`。

## 3. 原生控制层

`OpenLibSys.cs` 恢复出一个完整的 `OpenLibSys.Ols` 封装层，用于动态加载：

- `WinRing0.dll`
- `WinRing0x64.dll`

并暴露以下高价值硬件接口：

- `Cpuid` / `CpuidTx` / `CpuidPx`
- `Rdmsr` / `RdmsrTx` / `RdmsrPx`
- `Wrmsr` / `WrmsrTx` / `WrmsrPx`
- IO 端口与 PCI 访问

这说明该程序不是仅依赖托管层 API，而是直接下探到 CPU / MSR 级观测与控制。

## 4. 观测与采样层

`Service1.cs` 内可以直接确认以下观测路径：

- 创建 `TraceEventSession(\"ThreadSwitchSession\")`
- 订阅 `session.Source.Kernel.ThreadCSwitch`
- 通过 `System.Management` 和线程/进程信息收集额外上下文

这意味着调度器的输入并不只来自静态规则，而是持续消费线程切换和运行时状态。

## 5. 学习与推理层

当前恢复树中，调度和模型相关代码主要分成四组：

### 5.1 `SchedulerModel.cs`
- `CircularBuffer<T>`
- `DecisionRecord`
- `SchedulerStatistics`
- `TransformerScheduler`

这里是主调度核心和统计状态的聚合点。

### 5.2 `SchedulerService.cs`
- `SchedulerService`
- `SchedulerController`

这里提供更接近服务封装和状态报告的外层接口。

### 5.3 `TransformerLayers.cs`
- `MathHelper`
- `LinearLayer`
- `LayerNormLayer`
- `MultiHeadAttention`
- `FeedForwardLayer`
- `TransformerEncoderLayer`

这组类型说明作者并不是只做启发式规则，而是真正实现了类 Transformer 的推理组件。

### 5.4 `OnlineLearning.cs` 与 `OnlineLearningManager.cs`
- `MathCompat`
- `GradientHelper`
- `OnlineLearningManager`

这部分表明工程里同时保留了在线学习/梯度辅助逻辑。

## 6. 线程控制层

`Service1.cs` 中能直接找到：

- `SetThreadIdealProcessor`
- `SetThreadAffinityMask`

同时还能看到结合 `myOls.RdmsrTx(...)` / `myOls.WrmsrTx(...)` 的路径。这说明该服务不仅观察线程行为，而且会直接把线程推向不同核心集合。

从日志字符串和控制流看，它至少会维护：

- 大核/小核相关统计
- 当前调度结果
- 神经网络统计信息
- Attention 头报告

## 7. 当前最可信的整体分层

可以把 2.35 当前架构概括为四层：

1. 服务/安装层  
`Program`、`ProjectInstaller`、`Service1`

2. 观测与硬件接口层  
`OpenLibSys.Ols`、ETW 上下文切换采样、系统管理信息

3. 调度与模型层  
`TransformerScheduler`、`SchedulerService`、`SchedulerController`

4. 数学与学习支撑层  
`MathHelper`、`MatrixOperations`、`VectorMath`、`LinearLayer`、`MultiHeadAttention`、`OnlineLearningManager`

## 8. 与 2.34 的主要差异

当前 2.35 最显著的结构变化不是“换了一个服务入口”，而是工程边界更收敛：

- 2.34 发布包里，`CoreScheduler.dll` 和 `SimdLibrary.dll` 是单独 shipped 的组件。
- 2.35 当前 exe 反编译结果里，这些类型已经直接并入主工程。

这意味着 2.35 逆向时，最合理的高保真交付是单套主工程树，而不是继续沿用 2.34 的多项目混装恢复方式。

## 9. 仍需继续深挖的点

- `ThreadClassifier.cs` 等少数文件边界还缺少 sequence-point 级证据。
- `ProjectInstaller.resx` 已恢复，`IntlThrdPerfSchd.sln` 也已按推断型工程命名层补回；但原始 solution 细节和其他设计器附属资产仍缺直接证据。
- 如果要继续逼近 99% 原工程，需要进一步利用 DIA / metadata 做更细的方法到源码文件映射。
