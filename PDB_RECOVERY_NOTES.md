# Intel 大小核神经网络调度器 N 版 2.36 PDB 恢复说明

## 1. 直接证据
- shipped PDB 文件：
  - `Intel大小核神经网络调度器N版2.36\IntlThrdSchd\IntlThrdSchd.pdb`
- 从 PDB 字符串中提取到的原始源码根路径：
  - `C:\Users\maxpp\source\repos\IntlThrdPerfSchd4.6\IntlThrdPerfSchd2.22\IntlThrdPerfSchd\`
- 提取结果已保存到：
  - `recovery_artifacts/pdb_paths_2.36.txt`

## 2. PDB 直接暴露的高价值文件名
- `CoreIndexMapper.cs`
- `MatrixOperations.cs`
- `OnlineLearning.cs`
- `OnlineLearningManager.cs`
- `OpenLibSys.cs`
- `Program.cs`
- `ProjectInstaller.cs`
- `ProjectInstaller.Designer.cs`
- `RealtimeScheduler.cs`
- `SchedulerModel.cs`
- `SchedulerService.cs`
- `Service1.cs`
- `Service1.Designer.cs`
- `ThreadClassifier.cs`
- `ThreadDataPointV2.cs`
- `ThreadPerformanceTracking.cs`
- `TransformerLayers.cs`
- `VectorMath.cs`
- `VectorMathNew.cs`
- `obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs`

## 3. 本轮已执行的高置信恢复

### 3.1 文件布局扁平化
- 将 ILSpy 初始输出中的 `IntlThrdPerfSchd/`、`OpenLibSys/`、`SimdLibrary/` 子目录源码恢复到项目根目录，以贴近 PDB 暴露的原始路径风格。

### 3.2 文件名与组合文件恢复
- 已对齐文件名：
  - `OpenLibSys\Ols.cs` -> `OpenLibSys.cs`
  - `SimdLibrary\MatrixOperations.cs` -> `MatrixOperations.cs`
  - `SimdLibrary\VectorMath.cs` -> `VectorMath.cs`
  - `SimdLibrary\VectorMathNew.cs` -> `VectorMathNew.cs`
  - `SOMNeuron.cs` -> `ThreadClassifier.cs`
- 已按 2.35 已验证分组恢复组合文件：
  - `OnlineLearning.cs`
  - `SchedulerModel.cs`
  - `SchedulerService.cs`
  - `TransformerLayers.cs`
  - `ThreadPerformanceTracking.cs`
  - `RealtimeScheduler.cs`

### 3.3 partial / Designer 边界恢复
- 已恢复：
  - `ProjectInstaller.cs` / `ProjectInstaller.Designer.cs`
  - `Service1.cs` / `Service1.Designer.cs`
- 当前这两组文件已不再维持 ILSpy 初始输出的“组合文件”状态。

### 3.4 工程文件首轮收敛
- 项目文件已改名为：
  - `IntlThrdPerfSchd.csproj`
- 当前继续保持：
  - `AssemblyName = IntlThrdSchd`
  - `RootNamespace = IntlThrdSchd`
- 当前已修正外部引用路径到相对当前工程目录的正确位置。
- 当前已补回：
  - `ProjectInstaller.resx`
- 原因：
  - shipped EXE 当前明确只带一个 manifest resource：
    - `IntlThrdSchd.ProjectInstaller.resources`
  - 这与 2.35 的资源形态一致，说明 `ProjectInstaller.resx` 继续属于高置信应恢复资产。

### 3.5 DIA compiland / method map 第二阶段补强
- 当前已新增：
  - `recovery_artifacts/dia_method_map_2.36.json`
- 当前量化结果：
  - `compiland_count = 107`
  - `method_entry_count = 1842`
- 当前已被 DIA compiland 级证据直接确认的关键归位包括：
  - `CoreIndexMapper` -> `CoreIndexMapper.cs`
  - `MathCompat` / `GradientHelper` -> `OnlineLearning.cs`
  - `CircularBuffer<T>` / `DecisionRecord` / `SchedulerStatistics` / `TransformerScheduler` -> `SchedulerModel.cs`
  - `SchedulerService` / `SchedulerController` -> `SchedulerService.cs`
  - `RandomExtensions` -> `RealtimeScheduler.cs`
  - `SOMNeuron` -> `ThreadClassifier.cs`
  - `Tracker4lat` / `Tracker` / `ThreadPerformanceTracker` -> `ThreadPerformanceTracking.cs`
  - `CoreTransformerEncoder` / `ThreadTransformerEncoder` -> `TransformerLayers.cs`
  - `ProjectInstaller` -> `ProjectInstaller.cs + ProjectInstaller.Designer.cs`
  - `Service1` -> `Service1.cs + Service1.Designer.cs`
- 这说明当前 2.36 高保真树的关键组合文件和 partial / Designer 拆分，已经不只是“参考 2.35 的合理恢复”，而是被 shipped PDB 的 compiland 级 line-span 直接支撑。

### 3.6 Designer 资源与 solution 级验证
- 当前重新串行执行：
  - `dotnet build .\\recovered_src_2.36\\IntlThrdPerfSchd.sln -v minimal -m:1`
- 结果：
  - `0 warnings, 0 errors`
- 当前重新对比 `ProjectInstaller.resources`：
  - shipped EXE 内 `IntlThrdSchd.ProjectInstaller.resources`
  - 本地构建产物 `obj\\Debug\\net48\\IntlThrdSchd.ProjectInstaller.resources`
- 字节级结果：
  - `origLength = 180`
  - `newLength = 180`
  - `equal = True`
- 因而 `ProjectInstaller.resx` 当前已经提升为“编译产物与 shipped resource 字节级一致”的已验证恢复项。

### 3.7 Raw DIA sequence-point 补强
- 当前已新增：
  - `recovery_artifacts/scripts/extract-dia-sequence-points.py`
  - `recovery_artifacts/dia_sequence_points_2.36.json`
  - `recovery_artifacts/DIA_SEQUENCE_POINTS_2.36.md`
- 当前目标过滤下的量化结果：
  - `compiland_count = 2`
  - `sequence_point_count = 29`
- 当前已直接确认的 visible lines：
  - `ProjectInstaller.cs`
    - `14 / 16 / 17 / 22 / 27`
  - `ProjectInstaller.Designer.cs`
    - `16 / 18 / 20 / 21 / 31 / 32 / 36 / 37 / 38 / 42 / 43 / 44 / 45 / 46 / 47 / 51-53 / 55`
  - `Service1.Designer.cs`
    - `16 / 18 / 20 / 21 / 31 / 32 / 33`
- 当前高价值结论：
  - `ProjectInstaller.Designer.cs` 的 `AddRange(...)` 原稿继续更像跨 `51-53` 的多行 statement
  - `ProjectInstaller.Designer.cs` 的 line `31/32/47` 列宽继续强烈支持全限定名对象创建与显式委托事件绑定
  - `Service1.Designer.cs` 的 line `31` 列宽继续强烈支持 `this.components = new System.ComponentModel.Container();`
  - 2.36 这组模板化小文件的 raw sequence-point 形态当前与 2.35 保持同型

### 3.8 DIA InjectedSource 与 raw named-stream 补强
- 当前已新增：
  - `recovery_artifacts/dia_injected_source_2.36.json`
  - `recovery_artifacts/DIA_INJECTED_SOURCE_EVIDENCE_2.36.md`
  - `recovery_artifacts/DIA_INJECTED_SOURCE_PAYLOAD_2.36.md`
  - `recovery_artifacts/VS_TEMPLATE_EVIDENCE_2.36.md`
  - `recovery_artifacts/pdb_named_streams_2.36.json`
  - `recovery_artifacts/PDB_NAMED_STREAMS_2.36.md`
- 当前 `InjectedSource` 量化结果：
  - `SourceFiles = 20`
  - `InjectedSource = 20`
  - `LineNumbers = 12844`
  - `Symbols = 10978`
  - `all_compiland_count = 109`
  - 特殊 compiland 继续只有：
    - `* CompilerInfo *`
    - `<DanglingDocuments*223343bd-a859-41a3-90c0-9dfa101f1a95>`
- 当前 payload 级结果：
  - `row_count = 20`
  - `unique_payload_sha256_count = 20`
  - `unique_first_32_hex_count = 1`
  - `all_first_32_hex_identical = true`
  - `recovered_sha256_match_count = 3`
  - 已命中的文档：
    - `Program.cs`
    - `Service1.Designer.cs`
    - `obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs`
- 当前 raw named-stream 结果：
  - `named_stream_count = 24`
  - `source_file_named_stream_count = 20`
  - `zero_length_named_stream_count = 2`
  - `20/20` `/src/files/...` 与 `InjectedSource payload` 字节级一致
  - `/names` 继续只含 `20` 对原始/小写路径与 `1` 个空字符串 ID
  - `/src/headerblock` 当前是 `20` 条结构化 entry，全部 `entry_size = 40`、`compression = 101`
- 这说明：
  - 2.36 的 PDB 旁证链与 2.35 当前明显同型
  - 但文档集已从 `19` 个扩展到 `20` 个
  - `VectorMathNew.cs` 当前已经进入 2.36 的真实 PDB 文档集
  - `Program.cs` 当前已经实现文本级命中，可直接复用 2.35 已验证的原稿形态
  - `Service1.Designer.cs` 当前已经实现文本级命中，并且命中依据不是继续穷举，而是本机 Visual Studio 中文 `WindowsService` 模板的直接旁证
  - 当前 package 内仍没有额外 `.sln/.csproj` 正文或隐藏源码文本流

### 3.8.1 Service1.Designer.cs 的本机 VS 模板命中
- 当前本机 Visual Studio 安装路径：
  - `D:\Microsoft Visual Studio\18\Community`
- 当前命中的模板文件：
  - `D:\Microsoft Visual Studio\18\Community\Common7\IDE\ProjectTemplates\CSharp\Windows\2052\WindowsService\service1.designer.cs`
- 当前命中方式：
  - 仅替换 `$safeprojectname$ -> IntlThrdPerfSchd`
  - 以 `UTF-8 BOM + CRLF` 写回 `Service1.Designer.cs`
- 当前结果：
  - 工作树中的 `Service1.Designer.cs` 已与 embedded checksum
    - `55ebaa1a84f3b504cbb7d938d18cceca024495279e7904d47ee17c1a20e30b6b`
    完全一致
- 当前意义：
  - `Service1.Designer.cs` 已经从“sequence-point 强约束 + 模板搜索未命中”推进到“本机模板直接复现 + InjectedSource 校验命中”
  - 后续模板化小文件继续深挖时，可以把重点从它收窄到 `ProjectInstaller.cs` 和 `ProjectInstaller.Designer.cs`

### 3.8.2 ProjectInstaller 的 VS/DTE 生成链旁证
- 当前已新增：
  - `recovery_artifacts/scripts/generate-vs-dte-projectinstaller-sample.ps1`
  - `recovery_artifacts/vs_dte_projectinstaller_sample_2.36.json`
  - `recovery_artifacts/vs_dte_projectinstaller_roundtrip_2.36.json`
  - `recovery_artifacts/VS_TEMPLATE_EVIDENCE_2.36.md`
- 当前 raw item template 生成结果：
  - `ProjectInstaller.cs`
    - `ae1e0b260d7f61482007eb98456edff20d63066964c2bfea230890f72430fa6c`
  - `ProjectInstaller.Designer.cs`
    - `9fc15dee6d87fb73f3a92aeebc0b265f20cc50768ce64d36c094f584b67dc384`
- 两者都没有命中 2.36 的 embedded checksum：
  - `ProjectInstaller.cs`
    - `5eaf364e732ef11197449d06b1c09aa3cf61509b52cc2823b8c2880741455b9d`
  - `ProjectInstaller.Designer.cs`
    - `6013fffa85739d2796c81344ef609335bc21701eb69b3a85413c04e238e16140`
- 当前还做了一轮 DTE Designer round-trip：
  - 先生成临时 Windows Service + Installer 工程
  - 再把当前恢复树中的 `ProjectInstaller.cs` / `ProjectInstaller.Designer.cs` 覆盖进去
  - 然后通过 DTE 打开 Designer 视图并执行 `SaveAll`
- 当前 round-trip 后结果：
  - `ProjectInstaller.cs`
    - `19bf4c8dda5769e0b6b2079dbb9d56b25052c6565bbeb6273926be8cab336ed0`
  - `ProjectInstaller.Designer.cs`
    - `1cadf82b4326d144449eec21728e94b973bfeddd1fe6d5be9889a1ffff3e2c33`
- 这两个 hash 与当前恢复树文件完全一致，说明：
  - 当前恢复稿可以被 DTE / Designer 链接受
  - 但一次无新增设计操作的 round-trip 不会自动重写成 shipped 原稿文本
- 因而当前更合理的判断是：
  - `ProjectInstaller*` 的剩余文本差异，不是“直接替换成默认 VS item template”就能消掉
  - 也不是“把当前恢复稿丢给 Designer 再保存一次”就能自动消掉
  - 更可能仍然卡在真实设计器操作序列、事件绑定生成链或作者后续手工编辑壳层

### 3.9 编译器与语言级别补充证据
- shipped PDB 的原始字符串本轮又额外直接命中了：
  - `C# - 5.3.0-2.26153.122+4d3023de605a78ba3e59e50c657eed70f125c68a`
  - `obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs`
  - `/src/headerblock`
  - 多条 `/src/files/...`
- 这些命中说明当前 2.36 的 PDB 里也继续保留了 compiler/source-index 层痕迹。
- 同时，本轮也实际修正了一批明确属于反编译现代化噪音的语法：
  - 当前恢复树中的 `18` 个 `file-scoped namespace` 文件已全部恢复为 block-scoped namespace
  - 项目文件中的显式语言版本已从 `12.0` 收敛到 `8.0`
- 对这条证据链做 fresh build 后，当前结论是：
  - `LangVersion=7.3` 会失败
  - 失败点已经收敛到：
    - `VectorMath.cs` / `VectorMathNew.cs` 的 `Vector<T>` 相关“非托管构造类型”
    - `Service1.cs` 中的 `using` 声明
  - 当前工程文件与 solution 在 `8.0` 口径下都可以通过 fresh build
- 对应的详细记录已单独整理在：
  - `recovery_artifacts/COMPILER_LANGUAGE_FOOTPRINT_2.36.md`

### 3.10 Service1 method-map 补强
- 当前已新增：
  - `recovery_artifacts/SERVICE1_METHOD_MAP_2.36.json`
  - `recovery_artifacts/SERVICE1_METHOD_MAP_2.36.md`
- 当前量化结果：
  - `compiland_count = 63`
  - `method_entry_count = 1315`
  - `Service1.cs` entries = `1313`
  - `Service1.Designer.cs` entries = `2`
- 当前与 2.35 的归一化对比结果：
  - 总体规模完全一致
  - 归一化后只剩 `6` 处差异
- 当前 6 处差异全部集中在：
  - `OnStart`
  - `<OnStart>g__thread1|704_0`
  - `<OnStart>b__704_2`
  - `<OnStart>g__thread2|704_1`
  - `OnStop`
  - `OnTimedEvent`
- 这说明：
  - 2.36 的 `Service1` 总体拓扑当前并没有出现“大块重构”
  - 后续继续做 `Service1` 顺序逼近时，应把注意力集中到服务生命周期尾部，而不是整文件重排

### 3.11 Service1 生命周期尾部 raw sequence-point 对比
- 当前已新增：
  - `recovery_artifacts/service1_sequence_points_full_2.35_reference.json`
  - `recovery_artifacts/service1_sequence_points_full_2.36.json`
  - `recovery_artifacts/service1_lifecycle_tail_diff_2.35_vs_2.36.json`
  - `recovery_artifacts/SERVICE1_LIFECYCLE_TAIL_2.36.md`
- 当前最重要的结论分层如下：
  - `OnStart`
    - `262 -> 257`
    - 前 `16` 条 visible sequence points 完全一致
    - 第一处 line drift 出现在原始 line `17321 -> 17323`
    - 当前不是“整段乱序”，而是尾部真实变化后带出的复杂 tail realignment
  - `<OnStart>g__thread1|704_0`
    - 前 `3` 条完全一致
    - 后 `3` 条统一 `+2`
  - `<OnStart>b__704_2`
    - `365` 条里 `358` 条完全不变
    - 第一处 column 扩展发生在原始 line `17201`
      - `column_end: 59 -> 85`
    - 第二处关键 column 扩展发生在原始 line `17206`
      - `column_end: 111 -> 118`
  - `<OnStart>g__thread2|704_1`
    - `4/4` 条统一 `+3`
  - `OnStop`
    - `2/2` 条统一 `+3`
  - `OnTimedEvent`
    - `31/31` 条总数不变
    - 当前主要是 `+3` 偏移，剩余两类只是 line-range closing 口径差异
- 当前已能直接钉实的真实源码变化有两处：
  - `if (sysinfo.accQcount > 0)` -> 当前最强文本候选为 `if (sysinfo.accQcount > 0&&sysinfo.total_energy > 0)`
  - `transformerScheduler.UpdateTAT(sysinfo.accRewordPerS / sysinfo.accQcount);`
    -> 当前最强文本候选为 `transformerScheduler.UpdateTAT((float)sysinfo.accRewordPerS / sysinfo.accQcount);`
- 当前新增的列宽约束说明：
  - 原始 line `17206`：
    - 2.35 width = `74`
    - 2.36 target width = `81`
    - 双侧显式转换稿 width = `88`
    - 单侧显式转换稿 width = `81`
  - 因此 `UpdateTAT(...)` 这一行当前已从“显式 float 转换”进一步收敛到“只对分子做一次显式 float 转换”。
- guard 行当前也已进一步收敛：
  - 原始 line `17201`：
    - 2.35 width = `26`
    - 2.36 target width = `52`
    - 常规空格版本 width = `54`
    - `if (sysinfo.accQcount > 0&&sysinfo.total_energy > 0)` width = `52`
  - 当前候选枚举下，它已成为唯一自然且精确命中 target width 的 guard 文本候选。
- 因而当前 2.36 的 `Service1` 深挖重点已经进一步收敛为：
  - 不再做整文件顺序重排
  - 继续盯 `OnStart` 尾部相邻语句与 closing 归属的文本级恢复

## 4. 2.36 相对 2.35 的首轮新信号
- 源码根版本号前缀从：
  - `IntlThrdPerfSchd3.29`
  更新为：
  - `IntlThrdPerfSchd4.6`
- 当前类型清单中新增的显著顶层类型：
  - `CoreTransformerEncoder`
  - `ThreadTransformerEncoder`
  - `VectorMathNew`
- 这些类型当前都已进入恢复树：
  - `CoreTransformerEncoder` / `ThreadTransformerEncoder` 已并入 `TransformerLayers.cs`
  - `VectorMathNew` 保持独立文件 `VectorMathNew.cs`

## 5. 当前构建验证
- 执行：
  - `dotnet build .\recovered_src_2.36\IntlThrdPerfSchd\IntlThrdPerfSchd.csproj -v minimal -m:1`
- 结果：
  - `179 warnings, 0 errors`
- 另已串行执行：
  - `dotnet build .\recovered_src_2.36\IntlThrdPerfSchd.sln -v minimal -m:1`
- 结果：
  - `0 warnings, 0 errors`
- 当前 warning 主要仍是未使用字段和局部变量，整体更像源码遗留或反编译恢复后的真实状态，不宜为了“更干净”而贸然删改。

## 6. 当前仍未完成的深挖项
- 还未对 2.36 做：
  - 更深的 `Service1` 尾部生命周期方法顺序与 sequence-point 级收敛
  - `InjectedSource` checksum 驱动的其余 `17/20` 文档文本级逼近
  - solution / project GUID 恢复

## 7. 当前结论
- 2.36 当前最合理的恢复策略仍然是：
  - 单套 `net48` 主工程树
  - 不再回退到 2.34 那种多程序集 mixed bundle 恢复方式
- 这一版已经达到了“可构建、目录结构明显向原工程收敛”的第一阶段目标，后续可以在这棵树上继续做更深的 PDB/DIA 对齐。
