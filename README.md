# Intel 大小核神经网络调度器 N 版 2.36 高保真恢复树

本目录包含基于 2.36 发布包恢复出来的第一版高保真工程树，目标是尽量贴近原作者原始工程，而不是仅提供可阅读的反编译 dump。

## 目录说明
- `IntlThrdPerfSchd.sln`
  - 按旧式 `.sln` 格式补回的推断型解决方案文件
  - 命名依据来自 PDB 源码根目录最后一级 `IntlThrdPerfSchd`
- `IntlThrdPerfSchd/`
  - 当前主交付工程树
  - 目标框架为 `net48`
  - 当前项目文件名为 `IntlThrdPerfSchd.csproj`
  - 程序集名与资源根命名空间保持为 `IntlThrdSchd`
- `PDB_RECOVERY_NOTES.md`
  - 记录 2.36 shipped PDB 暴露的文件名、源码根路径和当前已经执行的高置信恢复
- `PROJECT_METADATA_RECOVERY.md`
  - 记录工程命名层恢复中哪些来自直接证据，哪些属于推断恢复
- `ARCHITECTURE_ANALYSIS.md`
  - 记录 2.36 的入口、观测、调度、模型和线程控制结构
- `recovery_artifacts/`
  - 保存 `type_list_2.36.txt`、`pdb_paths_2.36.txt` 等机器生成证据

## 当前状态
- 已完成首轮目录扁平化与文件归位：
  - `OpenLibSys\Ols.cs` 已对齐为 `OpenLibSys.cs`
  - `SimdLibrary\MatrixOperations.cs` / `VectorMath.cs` / `VectorMathNew.cs` 已恢复到项目根
  - `MathCompat + GradientHelper` 已并回 `OnlineLearning.cs`
  - `CircularBuffer + DecisionRecord + SchedulerStatistics + TransformerScheduler` 已并回 `SchedulerModel.cs`
  - `SchedulerService + SchedulerController` 已并回 `SchedulerService.cs`
  - `MathHelper + LinearLayer + LayerNormLayer + MultiHeadAttention + FeedForwardLayer + TransformerEncoderLayer + CoreTransformerEncoder + ThreadTransformerEncoder` 已并回 `TransformerLayers.cs`
  - `Tracker4lat + Tracker + ThreadPerformanceTracker` 已并回 `ThreadPerformanceTracking.cs`
  - `RandomExtensions` 已并回 `RealtimeScheduler.cs`
  - `SOMNeuron.cs` 已按 PDB 文件名对齐为 `ThreadClassifier.cs`
- 已完成明显 partial / Designer 边界恢复：
  - `ProjectInstaller.cs` / `ProjectInstaller.Designer.cs`
  - `Service1.cs` / `Service1.Designer.cs`
- 已完成 compiland 级 DIA 归位补强：
  - `recovery_artifacts/dia_method_map_2.36.json`
  - 当前量化结果：
    - `compiland_count = 107`
    - `method_entry_count = 1842`
  - 当前已被直接支撑的关键归位包括：
    - `MathCompat` / `GradientHelper` -> `OnlineLearning.cs`
    - `SchedulerService` / `SchedulerController` -> `SchedulerService.cs`
    - `CoreTransformerEncoder` / `ThreadTransformerEncoder` -> `TransformerLayers.cs`
    - `Tracker4lat` / `Tracker` / `ThreadPerformanceTracker` -> `ThreadPerformanceTracking.cs`
    - `RandomExtensions` -> `RealtimeScheduler.cs`
    - `ProjectInstaller` -> `ProjectInstaller.cs + ProjectInstaller.Designer.cs`
    - `Service1` -> `Service1.cs + Service1.Designer.cs`
- 已完成 Designer 资源恢复：
  - `ProjectInstaller.resx`
- 已完成 Designer 资源字节级校验：
  - 编译产物 `IntlThrdSchd.ProjectInstaller.resources` 与 shipped EXE 内同名 manifest resource 字节级一致
  - 当前验证结果：`origLength = 180`、`newLength = 180`、`equal = True`
- 已完成 3 个模板化小文件的 raw DIA sequence-point 提取：
  - `recovery_artifacts/dia_sequence_points_2.36.json`
  - `recovery_artifacts/DIA_SEQUENCE_POINTS_2.36.md`
  - 当前关键 visible lines：
    - `ProjectInstaller.cs` = `14 / 16 / 17 / 22 / 27`
    - `ProjectInstaller.Designer.cs` = `16 / 18 / 20 / 21 / 31 / 32 / 36 / 37 / 38 / 42 / 43 / 44 / 45 / 46 / 47 / 51-53 / 55`
    - `Service1.Designer.cs` = `16 / 18 / 20 / 21 / 31 / 32 / 33`
- 已完成 `Service1.Designer.cs` 的本机 VS 模板命中恢复：
  - `recovery_artifacts/VS_TEMPLATE_EVIDENCE_2.36.md`
  - 当前基于本机 `WindowsService\service1.designer.cs` 中文模板，仅替换 `$safeprojectname$ -> IntlThrdPerfSchd`，并按 `UTF-8 BOM + CRLF` 写回后，已与 2.36 shipped PDB 的 embedded checksum 完全一致
- 已完成 `ProjectInstaller*` 的 VS/DTE 生成链旁证：
  - `recovery_artifacts/vs_dte_projectinstaller_sample_2.36.json`
  - `recovery_artifacts/VS_TEMPLATE_EVIDENCE_2.36.md`
  - 当前结论：
    - raw `Installer` item template 直接生成的 `ProjectInstaller.cs` / `ProjectInstaller.Designer.cs` 都不命中 shipped checksum
    - 把当前恢复稿灌入临时工程后再走一次 DTE Designer round-trip，hash 仍保持为当前恢复稿，不会自动收敛到目标 checksum
- 已完成 2.36 的 `InjectedSource` 与 raw named-stream 级旁证：
  - `recovery_artifacts/dia_injected_source_2.36.json`
  - `recovery_artifacts/DIA_INJECTED_SOURCE_EVIDENCE_2.36.md`
  - `recovery_artifacts/DIA_INJECTED_SOURCE_PAYLOAD_2.36.md`
  - `recovery_artifacts/VS_TEMPLATE_EVIDENCE_2.36.md`
  - `recovery_artifacts/pdb_named_streams_2.36.json`
  - `recovery_artifacts/PDB_NAMED_STREAMS_2.36.md`
  - 当前量化结果：
    - `SourceFiles = 20`
    - `InjectedSource = 20`
    - `all_compiland_count = 109`
    - `named_stream_count = 24`
    - `20/20` `/src/files/...` 与 `InjectedSource payload` 字节级一致
  - 当前文本级 checksum 命中结果：
    - `recovered_sha256_match_count = 3`
    - 已命中文档：
      - `Program.cs`
      - `Service1.Designer.cs`
      - `obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs`
- 已完成编译器/语言级别噪音收敛：
  - 当前项目文件的显式 `LangVersion` 已从 `12.0` 收敛到 `8.0`
  - 当前恢复树里的 `18` 个 `file-scoped namespace` 已全部恢复为 block-scoped namespace
  - `LangVersion=7.3` fresh build 失败
  - 当前工程文件与 solution 在 `8.0` 口径下均可 fresh build 通过
  - 对应证据已单独整理在 `recovery_artifacts/COMPILER_LANGUAGE_FOOTPRINT_2.36.md`
- 已完成 `Service1*` method map 补强：
  - `recovery_artifacts/SERVICE1_METHOD_MAP_2.36.json`
  - `recovery_artifacts/SERVICE1_METHOD_MAP_2.36.md`
  - 当前量化结果：
    - `compiland_count = 63`
    - `method_entry_count = 1315`
    - `Service1.cs` entries = `1313`
    - `Service1.Designer.cs` entries = `2`
  - 当前与 2.35 的归一化对比结果：
    - 总体规模完全一致
    - 只剩 `6` 处尾部 line-span 差异
- 已完成 `Service1` 生命周期尾部 raw sequence-point 对比：
  - `recovery_artifacts/service1_sequence_points_full_2.35_reference.json`
  - `recovery_artifacts/service1_sequence_points_full_2.36.json`
  - `recovery_artifacts/service1_lifecycle_tail_diff_2.35_vs_2.36.json`
  - `recovery_artifacts/SERVICE1_LIFECYCLE_TAIL_2.36.md`
  - 当前最关键的新收敛：
    - `OnStop` 与 `thread2()` 已被证明只是统一 `+3` 偏移
    - `OnTimedEvent` 总体是稳定结构上的局部偏移，不是新的方法体重写
    - `OnStart` 尾部已经确认至少有两处真实表达式级变化：
      - `if (sysinfo.accQcount > 0)` -> 当前最强文本候选已收敛到 `if (sysinfo.accQcount > 0&&sysinfo.total_energy > 0)`
      - `transformerScheduler.UpdateTAT(...)` -> 当前最强文本候选已收敛到“只对分子做一次显式 `float` 转换”的版本
- 已完成 `OnStart` 尾部文本证据补强：
  - `recovery_artifacts/ONSTART_TAIL_TEXT_EVIDENCE_2.36.md`
  - 当前新增结论：
    - `UpdateTAT(...)` 行已依据 raw PDB 列宽从双侧显式转换收敛为单侧显式转换
    - guard 行已依据同一组列宽约束进一步收敛到 `&&` 两侧无空格的唯一自然候选
- 当前首轮构建验证已通过：
  - `dotnet build .\recovered_src_2.36\IntlThrdPerfSchd\IntlThrdPerfSchd.csproj -v minimal -m:1`
  - 结果：`179 warnings, 0 errors`
  - `dotnet build .\recovered_src_2.36\IntlThrdPerfSchd.sln -v minimal -m:1`
  - 结果：`0 warnings, 0 errors`

## 当前最重要的新发现
- 2.36 仍然维持 2.35 的 `net48` 单 exe 服务主线，而不是回退到 2.34 的多程序集 mixed bundle 恢复方式。
- 相比 2.35，2.36 的 shipped PDB 源码根已经从：
  - `IntlThrdPerfSchd3.29`
  变成：
  - `IntlThrdPerfSchd4.6`
- 相比 2.35，2.36 当前新出现的显著类型包括：
  - `CoreTransformerEncoder`
  - `ThreadTransformerEncoder`
  - `VectorMathNew`
- 这说明 2.36 不只是小修，Transformer 相关编码层和向量数学层继续扩展了。

## 当前边界
- 这一版已经是“可构建 + 文件边界更像原工程 + 关键 Designer 资源已字节级对齐 + 编译器噪音已明显收敛”的强骨架，但还不是最终 2.35 那种深挖完成态。
- 目前还没继续做的工作主要包括：
  - `OnStart` 尾部 energy/TAT 更新块周围相邻语句与 closing 归属的文本级逼近
  - `InjectedSource` payload checksum 驱动的其余 `17/20` 文档文本级逼近
  - solution / 工程元数据层的进一步恢复
  - `Service1` 尾部顺序量化与 `ProjectInstaller.*` 的小文件文本级逼近
  - `ProjectInstaller*` 当年真实 Designer 操作序列或更深的设计期序列化链恢复

## 当前建议
- 如果后续继续深挖 2.36，优先顺序应是：
  1. 继续用当前 sequence-point 工件压缩 `ProjectInstaller.*` 的文本壳层差距
  2. 继续深挖 `ProjectInstaller*` 的设计期生成链，而不是再把原始 VS item template 当成作者最终文本
  3. 直接围绕 `OnStart` 尾部相邻语句与 closing 归属做文本级恢复，而不是再做整文件级 `Service1` 重排
  4. 继续基于 `InjectedSource` 的嵌入式 `SHA-256` 做文本级逼近和编译器指纹收敛
