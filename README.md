# Intel 大小核神经网络调度器 N 版 2.35 高保真恢复树

本目录包含基于 2.35 发布包恢复出来的单套高保真工程树，目标是尽量贴近原作者原始工程，而不是仅提供可阅读的反编译 dump。

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
  - 记录 shipped PDB 暴露的文件名、已执行的高置信恢复和剩余中等置信区
- `PROJECT_METADATA_RECOVERY.md`
  - 记录工程命名层恢复中哪些来自直接证据，哪些属于推断恢复
- `recovery_artifacts/`
  - 保存机器生成的 PDB 路径提取等证据文件
  - 已新增 `SOURCE_ROOT_HIERARCHY_2.35.md`、`DIA_FULL_COMPILAND_MAP_2.35.json`、`SERVICE_NAME_EVIDENCE_2.35.md`、`PDB_XML_CORROBORATION_2.35.md`、`VS_METADATA_LIMITS_2.35.md`、`COMPILER_LANGUAGE_FOOTPRINT_2.35.md`、`DIA_INJECTED_SOURCE_EVIDENCE_2.35.md`、`DIA_INJECTED_SOURCE_PAYLOAD_2.35.md`、`PDB_NAMED_STREAMS_2.35.md`、`dia_injected_source_2.35.json` 与 `pdb_named_streams_2.35.json`
- 其中 source-root 分析已进一步确认 `CoreIndexMapper` 是无命名空间顶层文件，服务名证据链则单独区分了 `IntlThrdSchd` 主线与批处理脚本分叉
  - 同时服务名证据链也已补入 framework 运行时语义，确认 2.35 shipped 包内部真实存在 `Service1` vs `IntlThrdSchd` 的命名矛盾
- `ARCHITECTURE_ANALYSIS.md`
  - 当前版本的架构和运行路径分析

## 当前状态
- 已恢复为单套工程树，而不是双树或多项目混装树。
- 已完成高置信的文件边界恢复：
  - `OnlineLearning.cs`
  - `SchedulerModel.cs`
  - `SchedulerService.cs`
  - `TransformerLayers.cs`
  - `ProjectInstaller.cs` / `ProjectInstaller.Designer.cs` / `ProjectInstaller.resx`
  - `Service1.cs` / `Service1.Designer.cs`
- 已完成 compiland 级强证据归位：
  - `RandomExtensions` 已并回 `RealtimeScheduler.cs`
  - `Tracker` / `Tracker4lat` / `ThreadPerformanceTracker` 已并回 `ThreadPerformanceTracking.cs`
  - `SOMNeuron` 已对齐到 `ThreadClassifier.cs`
- 已完成 Designer 资源恢复：
  - `ProjectInstaller.resx` 编译后的 `.resources` 与 shipped `IntlThrdSchd.ProjectInstaller.resources` 字节级一致
- 已完成依赖与配置层校对：
  - `IntlThrdPerfSchd.csproj` 的显式程序集引用与 shipped EXE 引用表一致
  - `app.config` 与 shipped `IntlThrdSchd.exe.config` 无差异一致
- 已完成编译器/语言级别噪音收敛：
  - 已把 17 个 `file-scoped namespace` 文件恢复为旧式 block-scoped namespace
  - 当前项目文件的显式 `LangVersion` 已从 `12.0` 收敛到经 fresh build 验证可用的 `8.0`
  - 对应证据与验证已单独整理在 `COMPILER_LANGUAGE_FOOTPRINT_2.35.md`
- 已完成 DIA `InjectedSource` / 特殊 compiland 口径补强：
  - 当前 PDB 的 `InjectedSource` 已被证实不是 XML 独有现象，而是 DIA COM 直接可枚举的表
  - 当前 `SourceFiles.count = 19` 与 `InjectedSource.count = 19` 完全一致
  - 当前全量 `SymTagCompiland = 106`，其中额外 2 个特殊 compiland 是 `* CompilerInfo *` 与 `<DanglingDocuments*...>`
  - 对应证据已单独整理在 `DIA_INJECTED_SOURCE_EVIDENCE_2.35.md`
- 已完成 DIA `InjectedSource` payload 内容级解构：
  - 当前 `get_source(...)` 的完整 104 字节 payload 已可稳定通过原始 vtable 调用提取
  - `comtypes` 自动包装层只能稳定暴露 `cbData=0 -> (0,0)` 与 `cbData=1 -> (1,248)` 这类“长度 + 首字节”现象
  - 当前 payload 头可稳定解析为：
    - `CorSym_LanguageType_CSharp`
    - `CorSym_LanguageVendor_Microsoft`
    - `CorSym_DocumentType_Text`
    - `SHA256`
  - 当前 payload 还内嵌了每个源码文档自己的 `32` 字节 `SHA-256`
  - 当前恢复树里已有 2 个文件做到了与 payload 内嵌 checksum 的字节级一致：
    - `Program.cs`
    - `obj\\Release\\net48\\.NETFramework,Version=v4.8.AssemblyAttributes.cs`
  - 其余 `17` 个文件仍是高保真语义恢复而不是原始文本逐字节复刻
  - 对应证据已单独整理在 `DIA_INJECTED_SOURCE_PAYLOAD_2.35.md` 与 `dia_injected_source_2.35.json`
- 已完成 raw PDB named-stream 级旁证：
  - 当前 classic PDB 的 stream 1 named stream map 已被直接解析
  - 当前确认共有 `23` 个 named stream：
    - `19` 个 `/src/files/...`
    - `1` 个 `/src/headerblock`
    - `1` 个 `/names`
    - `2` 个零长度流 `/LinkInfo` / `/TMCache`
  - `19` 个 `/src/files/...` 流全部大小都是 `104` 字节，且与 `InjectedSource` 的 payload 做到 `19/19` 字节级一致
  - 这说明 `/src/files/...` 当前不是源码正文，而是源码校验元数据记录的 raw named-stream 镜像
  - 当前 `/names` 已进一步解码为：
    - `19` 条原始大小写路径
    - `19` 条全小写路径
    - `1` 个空字符串 ID
  - 当前 `/src/headerblock` 已进一步解码为：
    - `19` 条结构化文档元数据 entry
    - `object_name` 全为空字符串
    - `file_name` 为原始大小写路径
    - `virtual_file_name` 为对应全小写路径
  - 这说明当前 package 内这两条流也没有继续藏着 `.sln/.csproj/natvis` 正文之类额外工程资产
  - 对应证据已单独整理在 `PDB_NAMED_STREAMS_2.35.md` 与 `pdb_named_streams_2.35.json`
- 已完成推断型工程命名层恢复：
  - 项目文件名已对齐为 `IntlThrdPerfSchd.csproj`
  - 解决方案文件已补回为 `IntlThrdPerfSchd.sln`
  - 同时保留 `AssemblyName` / `RootNamespace = IntlThrdSchd`，以匹配 shipped 资源名
  - 并已追加 source-root 层级分析，确认所有带可见 line-span 的源码文档都收敛到同一个 `IntlThrdPerfSchd` 项目根
- 已确认一个容易误判的例外：
  - `CoreIndexMapper.cs` 虽然位于同一 project root 下，但它本身是无命名空间顶层类型，不应为了形式统一强行塞入 `IntlThrdPerfSchd` 命名空间
- 已确认一个更深层的发布物矛盾：
  - `Service1.InitializeComponent()` 在 shipped 二进制里确实写入 `base.ServiceName = "Service1"`
  - 而 `ProjectInstaller` 与安装链主线则一致指向 `IntlThrdSchd`
  - 当前高保真树选择保留这一不一致，并在 `SERVICE_NAME_EVIDENCE_2.35.md` 中单独说明
  - 同时结合 Win32 官方语义补充说明：在单服务 own-process 模型下，这个不一致未必会阻断启动
- 当前工程已能构建通过。

## 实际构建验证
在工作区根目录执行：

```powershell
dotnet build .\recovered_src_2.35\IntlThrdPerfSchd\IntlThrdPerfSchd.csproj -v minimal -m:1
```

本轮验证结果：
- `179 warnings`
- `0 errors`

## 保真原则
- 优先贴近源码布局和作者原始文件边界，而不是为了更“干净”去删减大量未使用字段或局部变量。
- 文档中会把“已证实恢复”和“中等置信推断”分开写，不把推断伪装成事实。

## 已知限制
- `Service1.cs` 当前在 shipped PDB 可恢复口径下已经做到 `bidirectional-adjusted exact matches = 260/260`；剩余差距主要不再是源码顺序，而是发布物未携带出来的工程元文件和构造函数洞级不可直接恢复项。
- `ProjectInstaller.resx` 已恢复并验证其编译产物与 shipped `IntlThrdSchd.ProjectInstaller.resources` 字节级一致；当前 `IntlThrdPerfSchd.sln` 与 `IntlThrdPerfSchd.csproj` 的命名属于推断恢复，不是二进制直接暴露。
- shipped 程序集本身没有暴露更多产品版本信息或额外资源名，因此后续继续提升保真度的空间主要集中在源码顺序细节和未随发布包携带出来的工程元文件。
- 当前关于 VS solution / project GUID 的恢复上限，已经单独整理在 `VS_METADATA_LIMITS_2.35.md`，用于避免后续重复投入同一类无新增证据的猜测恢复。
- 当前关于编译器/语言级别的恢复口径，已经单独整理在 `COMPILER_LANGUAGE_FOOTPRINT_2.35.md`，用于避免把反编译器引入的现代语法误当成原作者原始工程事实。
- 当前关于 DIA `InjectedSource` 与特殊 compiland 的补强证据，已经单独整理在 `DIA_INJECTED_SOURCE_EVIDENCE_2.35.md`，用于避免后续仍把这部分只当成 Ghidra XML 旁证。
- 当前关于 DIA `InjectedSource` payload 内容级解构的证据，已经单独整理在 `DIA_INJECTED_SOURCE_PAYLOAD_2.35.md` 与 `dia_injected_source_2.35.json`，用于后续做 machine-checkable 的源码文本级逼近。
  - 当前关于 raw PDB named stream 级旁证的证据，已经单独整理在 `PDB_NAMED_STREAMS_2.35.md` 与 `pdb_named_streams_2.35.json`，用于避免后续继续把 `/src/files/...` 误当成潜在源码文本流。
  - 当前关于 `/names` 与 `/src/headerblock` 的结构化解码，已经并入 `PDB_NAMED_STREAMS_2.35.md` 与 `pdb_named_streams_2.35.json`，用于避免后续继续把它们误当成潜在隐藏工程资产。
  - 当前关于 3 个模板化小文件的 checksum 搜索边界，已经单独整理在 `DESIGNER_CHECKSUM_SEARCH_NOTES_2.35.md`，用于避免后续重复跑同一批低价值模板空间。
  - 当前 `Program.cs` 已经依据 payload checksum 收敛为文本级命中版本，这也是当前除生成的 `.NETFramework,Version=v4.8.AssemblyAttributes.cs` 之外，第一个被确认逐字节对齐到原稿的源码文件。
