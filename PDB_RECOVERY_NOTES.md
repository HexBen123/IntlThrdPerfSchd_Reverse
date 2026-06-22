# Intel 大小核神经网络调度器 N 版 2.35 PDB 恢复说明

## 1. 直接证据
- shipped PDB 文件：`Intel大小核神经网络调度器N版2.35\IntlThrdSchd\IntlThrdSchd.pdb`
- 从 PDB 字符串中提取到的原始源码根路径：
  - `C:\Users\maxpp\source\repos\IntlThrdPerfSchd3.29\IntlThrdPerfSchd2.22\IntlThrdPerfSchd\`
- 提取结果已保存到：
  - `recovery_artifacts/pdb_paths_2.35.txt`

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

## 3. 本轮已执行的高置信恢复

### 3.1 文件布局扁平化
- 将 ILSpy 生成的 `IntlThrdPerfSchd/`、`OpenLibSys/`、`SimdLibrary/` 子目录内源码，按 PDB 暴露的原始路径风格，恢复到项目根目录。

### 3.2 组合文件恢复
- `OnlineLearning.cs`
  - `MathCompat`
  - `GradientHelper`
- `SchedulerModel.cs`
  - `CircularBuffer<T>`
  - `DecisionRecord`
  - `SchedulerStatistics`
  - `TransformerScheduler`
- `SchedulerService.cs`
  - `SchedulerService`
  - `SchedulerController`
- `TransformerLayers.cs`
  - `MathHelper`
  - `LinearLayer`
  - `LayerNormLayer`
  - `MultiHeadAttention`
  - `FeedForwardLayer`
  - `TransformerEncoderLayer`

这些映射不是主观命名，而是直接复用了 2.34 已完成的高保真参考树中同名文件的类组合方式，并与 2.35 当前类型集合逐一对应。

### 3.3 Designer / partial 恢复
- 已将 `ProjectInstaller.cs` 拆为：
  - `ProjectInstaller.cs`
  - `ProjectInstaller.Designer.cs`
- 已补回：
  - `ProjectInstaller.resx`
- 已将 `Service1.cs` 拆为：
  - `Service1.cs`
  - `Service1.Designer.cs`

拆分依据：
- 当前 2.35 反编译源码中，`ProjectInstaller.cs` 与 `Service1.cs` 都直接包含 `InitializeComponent()` 和 `Dispose(bool disposing)`。
- shipped PDB 也直接暴露了对应的 `.Designer.cs` 文件名。
- shipped `IntlThrdSchd.exe` 还直接包含 `IntlThrdSchd.ProjectInstaller.resources` manifest resource；恢复出的 `ProjectInstaller.resx` 编译后生成的 `.resources` 文件与 shipped 资源字节级一致。

## 4. 新增的 compiland 级强证据

本轮进一步使用 DIA `compiland -> source file` 映射后，以下结论已经从“字符串级高概率”提升为“高置信”：

- `CoreIndexMapper` -> `CoreIndexMapper.cs`
- `IntlThrdPerfSchd.RandomExtensions` -> `RealtimeScheduler.cs`
- `IntlThrdPerfSchd.Tracker4lat` -> `ThreadPerformanceTracking.cs`
- `IntlThrdPerfSchd.Tracker` -> `ThreadPerformanceTracking.cs`
- `IntlThrdPerfSchd.ThreadPerformanceTracker` -> `ThreadPerformanceTracking.cs`
- `IntlThrdPerfSchd.SOMNeuron` -> `ThreadClassifier.cs`
- `OpenLibSys.Ols` -> `OpenLibSys.cs`
- `SimdLibrary.MatrixOperations` -> `MatrixOperations.cs`
- `SimdLibrary.VectorMath` -> `VectorMath.cs`

因此当前源码树已经按上述证据完成了实际归位，而不是只停留在文档说明。
- 其中 `CoreIndexMapper` 需要单独说明：
  - shipped EXE 的类型清单直接显示它是 `Class CoreIndexMapper`
  - 也就是说它是一个无命名空间顶层类型
  - 它之前没有进入 `DIA_FULL_COMPILAND_MAP_2.35.json`，只是因为最初的 compiland 筛选条件只包含命名空间前缀，不是因为该文件没有可见 line-span
  - 进一步的 `pdb.exe` XML 旁证也直接包含 `CoreIndexMapper` compiland 和 `CoreIndexMapper.cs` 的 line records

## 5. 当前中等置信区
- `ThreadClassifier.cs` 文件名已经由 DIA compiland 映射钉实，但当前文件内只有 `SOMNeuron`，而真正的 `Service1.ThreadClassifier` 仍然证实属于 `Service1.cs`。
- 因此 `ThreadClassifier.cs` 当前已经是高置信文件名对齐，但其“为什么文件名与主类型名不一致”的历史原因仍未完全恢复。

## 6. 构建验证
- 当前工程执行以下命令可通过构建：
  - `dotnet build .\IntlThrdPerfSchd.csproj -v minimal -m:1`
- 本轮实际结果为：
  - `179 warnings, 0 errors`

## 6.1 资源与程序集元数据补充证据
- shipped `IntlThrdSchd.exe` 当前确认只有一个 manifest resource：
  - `IntlThrdSchd.ProjectInstaller.resources`
- 恢复树中的 `ProjectInstaller.resx` 编译后生成的 `obj\Debug\net48\IntlThrdSchd.ProjectInstaller.resources` 与 shipped 资源字节级一致。
- 再次扫描 shipped `PDB/EXE`，未发现以下直接线索：
  - `.sln`
  - `.licx`
  - `app.manifest`
  - `Settings.settings`
  - 其他 `.resx`
  - 图标/位图等额外资源文件名
- shipped `FileVersionInfo` 也基本为空，版本信息为 `0.0.0.0`，没有公司/产品描述类字段；这与当前极简 `AssemblyInfo.cs` 形态相一致。
- shipped `IntlThrdSchd.exe` 的程序集引用表已与当前 `IntlThrdPerfSchd.csproj` 逐项对照，当前显式引用与真实发布物引用一致。
- 当前 `app.config` 与 shipped `IntlThrdSchd.exe.config` 也已比对为无差异一致。

## 6.2 推断型工程命名层恢复
- 当前项目文件名已恢复为：
  - `IntlThrdPerfSchd.csproj`
- 当前解决方案文件已补回为：
  - `recovered_src_2.35\IntlThrdPerfSchd.sln`
- 该恢复的依据链是：
  - shipped PDB 暴露的源码根路径最后一级目录明确是 `IntlThrdPerfSchd`
  - 2.34 的已完成 `pdb_aligned_src` 参考树也使用 `IntlThrdPerfSchd.csproj`
  - shipped `IntlThrdSchd.exe` 内的 manifest resource 名为 `IntlThrdSchd.ProjectInstaller.resources`，因此当前项目文件显式保留 `RootNamespace = IntlThrdSchd`
- 同时需要明确：
  - 当前再次扫描 shipped `PDB/EXE`，仍未命中任何 `.csproj` / `.sln` 直接字符串
  - 因此 `IntlThrdPerfSchd.sln` 的名称、内部 project GUID 与 solution 文件细节都属于“最接近原工程的推断恢复”，而不是 PDB 直接证据
  - 本轮进一步生成的 `DIA_FULL_COMPILAND_MAP_2.35.json` 与 `SOURCE_ROOT_HIERARCHY_2.35.md` 已确认：
    - 带可见 line-span 的 compiland 只落回单一 project root
    - classic PDB 的 `SymTagCompilandEnv` 为空
    - 因而当前仍不足以把 solution 文件名继续收窄到 `IntlThrdPerfSchd2.22.sln` 或 `IntlThrdPerfSchd3.29.sln`
  - `PDB_XML_CORROBORATION_2.35.md` 又进一步确认：
    - Ghidra `pdb.exe` 导出的 XML 同样未命中任何 `.sln` / `.csproj`
    - 因而这条结论已经得到第二套 PDB 解析路径的独立旁证
  - `VS_METADATA_LIMITS_2.35.md` 已将当前恢复上限单独收口：
    - 当前证据能恢复到 project root / project naming 层
    - 但仍不足以恢复原始 solution GUID / project GUID
  - 同时 `SERVICE_NAME_EVIDENCE_2.35.md` 已确认：
    - `IntlThrdSchd` 才是当前安装链更一致的主服务名
    - `IntlThrdPerfSchd` 主要来自批处理包装脚本分叉，不宜反向灌回主工程源码
    - shipped EXE 本体又确实把 `Service1.InitializeComponent()` 里的 `ServiceBase.ServiceName` 写成了 `Service1`
    - 结合本地 .NET Framework `ServiceBase` 实现与 Win32 官方语义，这更准确地构成“发布物内部仍未消解的服务名不一致”，但在当前服务模型下未必构成启动阻断

## 6.3 编译器与语言级别补充证据
- shipped PDB 的原始字符串本轮又额外直接命中了：
  - `C# - 5.3.0-2.26153.122+4d3023de605a78ba3e59e50c657eed70f125c68a`
  - `obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs`
  - `/src/headerblock`
  - 多条 `/src/files/...`
- 这些命中说明当前 PDB 里还保留了 compiler/source-index 层痕迹。
- 同时，本轮也实际修正了一批明显属于反编译现代化噪音的语法：
  - 17 个 `file-scoped namespace` 文件已恢复为 block-scoped namespace
  - 项目文件中的显式语言版本已从 `12.0` 收敛到 `8.0`
- 对这条证据链做 fresh build 后，当前结论是：
  - `LangVersion=7.3` 会失败
  - `LangVersion=8.0` 可以通过
  - 因而 `12.0` 已可确认为偏高恢复值，不宜继续保留
- 对应的详细记录已单独整理在：
  - `recovery_artifacts/COMPILER_LANGUAGE_FOOTPRINT_2.35.md`

## 6.4 DIA InjectedSource 与特殊 Compiland 补充证据
- 本轮继续直接使用 `pydia2 + Dia2Lib` 枚举 `IDiaSession.getEnumTables()` 后，当前已确认：
  - `InjectedSource` 是 DIA COM 层直接存在的真实表，不是 Ghidra XML 独有产物
  - 当前表计数为：
    - `SourceFiles = 19`
    - `InjectedSource = 19`
    - `LineNumbers = 9493`
    - `Sections = 1732`
    - `Symbols = 8647`
- 当前进一步确认：
  - `InjectedSource.fileName` 与 `SourceFiles.fileName` 的集合完全一致
  - `only_in_source = 0`
  - `only_in_injected = 0`
- 当前 `InjectedSource` 里包含的文件集合，与当前已知 18 个源码文件再加：
  - `obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs`
  完全一致
- 当前 `InjectedSource` 记录还稳定暴露出：
  - `objectFileName = ""`
  - `length = 0x68`
  - `sourceCompression = 101`
  - `virtualFilename` 为同一路径的小写版本
- 同时，本轮对 `SymTagCompiland` 全量枚举后确认：
  - 全量 compiland 总数是 `106`
  - 其中额外 2 个特殊 compiland 是：
    - `* CompilerInfo *`
    - `<DanglingDocuments*223343bd-a859-41a3-90c0-9dfa101f1a95>`
- 因而之前 `104` 的 line-span 口径现在可以更精确地解释为：
  - `104` 个源码相关 compiland
  - `2` 个 PDB 内部特殊 compiland
- 对应详细记录已单独整理在：
  - `recovery_artifacts/DIA_INJECTED_SOURCE_EVIDENCE_2.35.md`

## 6.5 DIA InjectedSource payload 内容级解构
- 本轮进一步确认：`IDiaInjectedSource.get_source(...)` 的 payload 已经可以稳定提取，但需要绕过 `comtypes` 的自动解组。
- 当前 `comtypes` 侧稳定观测到：
  - `get_source(0) -> (0, 0)`
  - `get_source(1) -> (1, 248)`
- 其中第二个值当前更应解释为：
  - `pbData` 首字节 `0xF8`
  - 而不是可直接使用的完整缓冲区地址
- 直接通过 `IDiaInjectedSource` 原始 vtable slot `9` 读取后，当前每条 payload 都稳定是 `104` 字节。
- 当前 payload 的前 64 字节可稳定解析为 4 个 GUID：
  - `CorSym_LanguageType_CSharp`
  - `CorSym_LanguageVendor_Microsoft`
  - `CorSym_DocumentType_Text`
  - `SHA256`
- 当前 payload 的后半段稳定表现为：
  - `checksum_length = 32`
  - `reserved_dword = 0`
  - `embedded_checksum_hex = 32-byte SHA-256`
- 当前 19 条记录的量化结果：
  - `row_count = 19`
  - `unique_payload_sha256_count = 19`
  - `unique_first_32_hex_count = 1`
  - `recovered_sha256_match_count = 2`
- 这说明：
  - 当前 19 条记录共享相同的语言/厂商/文档类型/哈希算法头
  - 但每条记录内嵌的源码哈希都不同
  - 当前恢复树里已有 2 个文件做到了与 payload 内嵌 checksum 的字节级一致：
    - `Program.cs`
    - `obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs`
  - 其中 `Program.cs` 已进一步确认原始文本形态更接近“旧式 Windows Service 模板”：
    - `UTF-8 BOM`
    - `CRLF`
    - 6 条默认 `using`
    - 中文 XML summary 注释
    - `ServicesToRun = new ServiceBase[] { ... }` 的标准模板写法
  - 其余 `17` 个文件当前仍应视为高保真语义恢复，而不是原始文本逐字节复刻
- 对应结构化工件与说明已新增：
  - `recovery_artifacts/dia_injected_source_2.35.json`
  - `recovery_artifacts/DIA_INJECTED_SOURCE_PAYLOAD_2.35.md`

## 6.6 Raw named-stream 级旁证
- 本轮进一步绕开 DIA 和字符串搜索，直接按 classic PDB 的 MSF 7.00 superblock、directory stream 和 stream 1 named stream map 做了 raw 解析。
- 当前确认：
  - named stream 总数是 `23`
  - `stream 5 = /LinkInfo`，大小 `0`
  - `stream 6 = /TMCache`，大小 `0`
  - `stream 7 = /names`，大小 `4412`
  - `stream 8 = /src/headerblock`，大小 `924`
  - `19` 个 `/src/files/...` 流稳定落在 `stream 11..29`
- 当前最关键的新结论是：
  - `19` 个 `/src/files/...` 流的大小全部都是 `104`
  - 它们与 `recovery_artifacts/dia_injected_source_2.35.json` 中同名文档的 `payload_hex` 做到 `19/19` 字节级完全一致
  - 因而 `/src/files/...` 当前不应再被视为“可能还没正确解压出来的源码正文”，而应视为与 `InjectedSource` 相同的源码校验元数据镜像
- 本轮进一步把 `/names` 与 `/src/headerblock` 也解码后，当前又确认：
  - `/names` 的 `signature = 0xEFFEEFFE`
  - `/names` 当前只含 `39` 个 name id：
    - `19` 条原始大小写路径
    - `19` 条全小写路径
    - `1` 个空字符串 ID
  - `/names` 中当前没有额外 `.sln`、`.csproj`、natvis 文件名或其他工程元数据字符串
  - `/src/headerblock` 当前 `table_size = 19`
  - `19` 条 entry 全部满足：
    - `entry_size = 40`
    - `compression = 101`
    - `is_virtual = 0`
    - `object_name` 为空字符串
    - `file_name` 是原始大小写路径
    - `virtual_file_name` 是对应全小写路径
  - 这说明 `/names` 与 `/src/headerblock` 当前也只是围绕同一批 19 个文档元数据做索引，没有再暴露额外工程资产
- 这条证据进一步收紧了后续恢复边界：
  - 继续深挖 `/src/files/...` 本身不会直接吐出源码文本
  - 更合理的后续方向已经收敛为继续用内嵌 `SHA-256` 反推文本形态
- 对应结构化工件与说明已新增：
  - `recovery_artifacts/pdb_named_streams_2.35.json`
  - `recovery_artifacts/PDB_NAMED_STREAMS_2.35.md`

## 7. Service1 顺序保真度
- `SERVICE1_METHOD_MAP_2.35.json` 已成功从 shipped PDB + dnlib token 解析生成。
- `SERVICE1_ORDER_DRIFT_2.35.json` / `.md` 已生成。
- 当前主要量化指标：
  - `current_decl_count = 261`
  - `original_mapped_count = 269`
  - `matched_count = 260`
  - `spearman_rank_correlation = 1.0000`
  - `constructor-hole-adjusted exact matches = 257/260`
  - `bidirectional-adjusted exact matches = 260/260`
- 说明：
  - 当前 `Service1.cs` 的顶层块顺序已经完成高漂移区重排，并与 2.34 已对齐树呈现同一拓扑顺序。
  - 进一步补回 `ThreadLoadManager4b/4l` 的显式外层构造函数，并将 `Count` 属性顺序校正后，当前 `SERVICE1_ORDER_DRIFT_2.35.md` 已达到：
    - `bidirectional-adjusted non-zero drift rows = 0`
    - `bidirectional-adjusted max abs drift = 0`
  - 这意味着在当前 shipped PDB 可恢复口径下，`Service1` 顺序已经达到完全一致。
  - 仍保留的 `constructor-hole-adjusted` 微小偏差主要来自构造函数洞级校正口径，而不是源码块本身仍放错位置。

## 8. 当前结论
- 当前工程树已经从“ILSpy 默认输出”收敛到“单套、可构建、且文件边界明显更贴近 PDB 证据”的状态。
- 后续若继续逼近 99% 原始工程，最值得继续投入的点是：
  - 原始 `.sln` 细节、未暴露的 Designer 附属资产或其他工程级元文件恢复
