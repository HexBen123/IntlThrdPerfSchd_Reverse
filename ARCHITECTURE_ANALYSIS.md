# Intel 大小核神经网络调度器 N 版 2.36 架构分析

## 1. 结论摘要
- 2.36 当前最可信的结构判断仍然是：
  - 这是一个以 `IntlThrdSchd.exe` 为主入口的 `net48` Windows 服务工程。
  - 它继续沿用 2.35 的“单 exe 主工程”结构，而不是 2.34 的多程序集拆分形态。
  - 它仍然同时覆盖服务安装、ETW 采样、WinRing0/MSR 访问、线程特征分析、神经网络推理和线程亲和性控制。
- 相比 2.35，2.36 的显著新信号是：
  - `CoreTransformerEncoder`
  - `ThreadTransformerEncoder`
  - `VectorMathNew`
  这些类型的出现说明模型编码层与向量数学层仍在扩展。

## 2. 服务入口与安装链路
- 当前运行主线仍然是：
  - `Program.Main()`
  - `ServiceBase.Run(new Service1())`
  - `Service1` 负责初始化和长期驻留
- 当前安装脚本层仍然围绕：
  - `IntlThrdSchd.exe`
  - `安装服务.bat`
  - `卸载服务.bat`
- 批处理包装脚本里的服务名当前仍使用：
  - `IntlThrdPerfSchd`
  这和 2.35 的命名层现象一致，说明项目/源码层与发布/服务层继续并存两套词形。

## 3. 原生接口层
- `OpenLibSys.cs` 恢复出完整的 `OpenLibSys.Ols` 封装层，用于动态加载：
  - `WinRing0.dll`
  - `WinRing0x64.dll`
- 这条线继续说明 2.36 不是只改 Windows 调度提示位的小工具，而是直接下探到 CPU / MSR / 驱动级接口。

## 4. 观测与采样层
- `Service1.cs` 仍然直接包含：
  - ETW / TraceEvent 相关路径
  - 线程与进程信息收集
  - 运行时统计与周期性定时逻辑
- 这说明 2.36 的调度输入仍然来自实时观测，而不是单纯静态规则。

## 5. 调度与模型层

### 5.1 `SchedulerModel.cs`
- 当前已并入：
  - `CircularBuffer<T>`
  - `DecisionRecord`
  - `SchedulerStatistics`
  - `TransformerScheduler`
- 这里是主调度核心和统计状态的聚合点。

### 5.2 `SchedulerService.cs`
- 当前已并入：
  - `SchedulerService`
  - `SchedulerController`
- 这里更偏向服务封装、调度外层接口和状态报告。

### 5.3 `TransformerLayers.cs`
- 当前已并入：
  - `MathHelper`
  - `LinearLayer`
  - `LayerNormLayer`
  - `MultiHeadAttention`
  - `FeedForwardLayer`
  - `TransformerEncoderLayer`
  - `CoreTransformerEncoder`
  - `ThreadTransformerEncoder`
- 相比 2.35，这里最重要的新信号是：
  - 编码器不再只有单个 `TransformerEncoderLayer`
  - 而是明确出现了面向不同输入域的 `Core` / `Thread` 编码器

### 5.4 `OnlineLearning.cs` 与 `OnlineLearningManager.cs`
- 当前已恢复：
  - `MathCompat`
  - `GradientHelper`
  - `OnlineLearningManager`
- 这说明 2.36 依然同时保留了在线学习/奖励窗口/统计链路。

## 6. 数学支撑层
- 当前数学与向量支撑代码位于：
  - `MatrixOperations.cs`
  - `VectorMath.cs`
  - `VectorMathNew.cs`
- `VectorMathNew.cs` 是 2.36 当前相对 2.35 的显著新增点，说明作者在 SIMD / 向量计算层又加了一套新实现，而不是只在原 `VectorMath.cs` 上微调。

## 7. 线程控制层
- `Service1.cs` 当前仍然包含：
  - `SetThreadIdealProcessor`
  - `SetThreadAffinityMask`
  - 以及围绕核心集合、组调度、大小核分配的复杂控制路径
- 因此 2.36 继续符合之前的总判断：
  - 它是一个主动控制线程落核的调度系统
  - 而不是被动观察或只做轻量建议

## 8. 当前最可信的整体分层
1. 服务/安装层  
`Program`、`ProjectInstaller`、`Service1`

2. 观测与硬件接口层  
`OpenLibSys.Ols`、ETW/TraceEvent、系统管理信息、定时采样

3. 调度与模型层  
`TransformerScheduler`、`SchedulerService`、`SchedulerController`

4. 数学与学习支撑层  
`MatrixOperations`、`VectorMath`、`VectorMathNew`、`LinearLayer`、`MultiHeadAttention`、`CoreTransformerEncoder`、`ThreadTransformerEncoder`、`OnlineLearningManager`

## 9. 当前仍需继续深挖的点
- `Service1` 的方法级顺序、Designer sequence points 和更细文件边界，还没有像 2.35 那样用 DIA 继续钉死。
- 2.36 的 manifest resources、`ProjectInstaller.resx`、`InjectedSource` payload、named streams 和编译器指纹都还没有继续抽。
- 如果要继续逼近 2.35 那种高保真终态，下一步应优先转到 DIA / metadata / source-index 级深挖。
