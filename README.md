# Intel 大小核神经网络调度器 N 版 2.51 高保真恢复树

本目录包含基于 2.51 发布包反编译/逆向恢复出来的高保真工程树，目标是尽量贴近原作者的工程结构与源码组织方式，而不是只留下可阅读的反编译 dump。

需要明确的一点是：2.51 发布包没有随包提供 `IntlThrdSchd.pdb`，并且 `IntlThrdSchd.exe` 内也没有发现 `RSDS/NB10` 这类 CodeView 调试目录签名或 `.pdb` 路径字符串，因此无法像 2.35/2.36 那样用 PDB/DIA 做“源码文件归属”和“文本级校验”。本恢复树采用的是“结构模板 + 语义同步 + 可审计证据”的策略：以 2.36 已验证的作者式文件边界作为模板，将 2.51 的真实实现同步进这些文件边界，并用实体清单对比与二进制字符串扫描做旁证。

## 目录说明
- `IntlThrdPerfSchd.sln`
  - 推断型解决方案入口（便于 VS 打开与构建）
- `IntlThrdPerfSchd/`
  - 主要交付工程树（`net48`）
  - 项目文件：`IntlThrdPerfSchd.csproj`
  - 程序集名与资源根命名空间：`IntlThrdSchd`（用于匹配 shipped manifest resource 名称）
- `_dnspy_export/`
  - dnSpy 导出的反编译证据
- `_ilspy_export/`
  - ILSpy 导出的反编译证据（默认语言版本）
- `_ilspy_export_cs8/`
  - ILSpy 导出的反编译证据（强制 C# 8 口径，减少现代语法噪音）
- `recovery_artifacts/`
  - 机器生成的证据与对比结果（实体清单、差异、字符串扫描等）
- `shipped_payload/`
  - 从 2.51 发布包中拷贝出来的关键运行时文件（便于在不依赖原始发布目录的情况下做运行/比对）

## 当前状态（2.51）
- 已完成“作者式组合文件”归位（以 2.36 的已验证分组为模板）：
  - `MathCompat + GradientHelper` -> `OnlineLearning.cs`
  - `CircularBuffer + DecisionRecord + SchedulerStatistics + TransformerScheduler` -> `SchedulerModel.cs`
  - `SchedulerService + SchedulerController` -> `SchedulerService.cs`
  - `MathHelper + LinearLayer + LayerNormLayer + MultiHeadAttention + FeedForwardLayer + TransformerEncoderLayer + CoreTransformerEncoder + ThreadTransformerEncoder` -> `TransformerLayers.cs`
  - `Tracker4lat + Tracker + ThreadPerformanceTracker` -> `ThreadPerformanceTracking.cs`
  - `RandomExtensions` -> `RealtimeScheduler.cs`
  - `SOMNeuron` -> `ThreadClassifier.cs`
- 已恢复明显的 partial / Designer 边界：
  - `Service1.cs` / `Service1.Designer.cs`
  - `ProjectInstaller.cs` / `ProjectInstaller.Designer.cs`
- 已通过可构建验证：
  - `dotnet build .\\recovered_src_2.51\\IntlThrdPerfSchd.sln -v minimal -m:1`
  - 当前结果：`0 warnings, 0 errors`

## 证据与一致性检查
- 实体清单对比（原始 2.51 vs 本恢复树构建产物）：
  - `recovery_artifacts/entities_original_2.51.txt`
  - `recovery_artifacts/entities_recovered_2.51.txt`
  - `recovery_artifacts/entities_diff_2.51.txt`
  - 当前差异只剩 1 处“编译器生成的闭包 DisplayClass 编号漂移”（属于编译器/方法顺序差异，非业务类型缺失）。
- 二进制字符串/调试信息扫描：
  - `recovery_artifacts/binary_string_scan_2.51.txt`
  - 关键结论：
    - 未发现 `RSDS/NB10` 或 `.pdb/.cs/.sln/.csproj` 相关字符串
    - 发现 `IntlThrdSchedErrorInfo.txt`（UTF-16LE）等运行时落盘文件名

## 复现反编译与对比（最小闭环）
- ILSpy（项目模式）导出：
  - `ilspycmd --disable-updatecheck -p -o recovered_src_2.51\\_ilspy_export_cs8 --languageversion CSharp8_0 <path-to-IntlThrdSchd.exe>`
- 实体清单导出：
  - `ilspycmd --disable-updatecheck -l cisde <assembly>`

## 权重文件说明（2.51 永久权重）
- `Service1` 默认使用 `./scheduler_model.bin` 作为模型文件路径；若文件不存在，调度器会用随机初始化继续运行。
- 2.51 发布包中附带的是 `scheduler_model.bin.bak`，已拷贝到 `shipped_payload/scheduler_model.bin.bak` 作为证据与备份。
- 如果你希望以该“永久权重”启动，通常需要把 `scheduler_model.bin.bak` 复制/重命名为 `scheduler_model.bin` 放到程序工作目录（与 `IntlThrdSchd.exe` 同级）。
