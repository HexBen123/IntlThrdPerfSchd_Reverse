# 2.35 DIA InjectedSource 与特殊 Compiland 证据

## 结论
- 当前 2.35 shipped classic PDB 的 DIA `EnumTables` 里，明确存在：
  - `InjectedSource`
  - `SourceFiles`
  - `Symbols`
  - `LineNumbers`
  - `Sections`
  - `SegmentMap`
  - `FrameData`
  - `InputAssemblyFiles`
- 其中 `InjectedSource` 不是 XML 导出器的独有产物，而是 **DIA COM 层直接可枚举** 的真实表。
- 当前 `InjectedSource.count = 19`，并且与 `SourceFiles.count = 19` 完全一一对应。
- 全量 `SymTagCompiland` 当前总数是 `106`，比之前 line-span 口径里的 `104` 多出 2 个特殊 compiland：
  - `* CompilerInfo *`
  - `<DanglingDocuments*223343bd-a859-41a3-90c0-9dfa101f1a95>`
- 这说明 2.35 PDB 的“真实 compiland 全景”现在已经不只是“104 个可见源码 compiland”，而是：
  - `104` 个源码相关 compiland
  - `2` 个 PDB 内部特殊 compiland

## 方法
- 使用本地 `pydia2` 环境通过 `Dia2Lib` 直接加载：
  - `Intel大小核神经网络调度器N版2.35\IntlThrdSchd\IntlThrdSchd.pdb`
- 使用：
  - `IDiaSession.getEnumTables()`
  - `IDiaEnumSourceFiles`
  - `IDiaEnumInjectedSources`
  - `globalScope.findChildren(SymTagCompiland, ...)`
- 目的：
  - 把之前基于 Ghidra `pdb.exe` XML 导出的 `InjectedSource` 发现，提升为 DIA COM 级证据

## EnumTables 结果
- `SourceFiles = 19`
- `LineNumbers = 9493`
- `Sections = 1732`
- `SegmentMap = 2`
- `InjectedSource = 19`
- `FrameData = 0`
- `InputAssemblyFiles = 0`
- `Symbols = 8647`

这组结果说明：
- 当前 PDB 中没有 input assembly sidecar 记录
- 当前也没有 frame data
- 但 source file / injected source / symbols / line numbers 四类表都是真实存在的

## InjectedSource 结果
- `InjectedSource.count = 19`
- `SourceFiles.count = 19`
- 两者文件名集合完全一致：
  - `only_in_source = 0`
  - `only_in_injected = 0`

### 当前可确认的 InjectedSource 形态
- 每条记录都包含：
  - `fileName`
  - `virtualFilename`
  - `crc`
  - `length`
  - `sourceCompression`
- 当前观测到的稳定模式：
  - `objectFileName` 为空字符串
  - `length = 0x68`
  - `sourceCompression = 101`
  - `virtualFilename` 是同一路径的全小写版本

### 已确认进入 InjectedSource 的文件
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
- `obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs`

## 特殊 Compiland
- 当前全量 `SymTagCompiland` 数量：
  - `106`
- 当前可见的特殊 compiland 只有 2 个：
  - `* CompilerInfo *`
  - `<DanglingDocuments*223343bd-a859-41a3-90c0-9dfa101f1a95>`

### 对现有口径的修正
- 之前 `DIA_FULL_COMPILAND_MAP_2.35.json` 的 `104`，是“带可见源码归属 / line-span 的源码 compiland”口径。
- 当前新增的 `106`，是“全量 SymTagCompiland”口径。
- 两者并不冲突，而是层级不同：
  - `104` 个源码相关 compiland
  - `2` 个 PDB 内部特殊 compiland

## 对恢复树的影响
- 这轮新证据进一步加固了以下结论：
  - 当前源码主文件集合确实已经基本完整
  - `obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs` 确实属于 PDB 中的真实文档集，而不是字符串噪音
  - `* CompilerInfo *` 不是单纯的原始字符串巧合，而是 DIA `Symbols` / `Compiland` 层也能看到的真实特殊符号
  - `<DanglingDocuments*...>` 说明当前 PDB 内部还存在 dangling document 容器，但没有额外把 `.sln` / `.csproj` 之类 VS 元数据吐出来

## 当前仍不能确认的点
- `sourceCompression = 101` 的具体算法名
- 原始 `.sln`
- 原始 project GUID / solution GUID

## 当前意义
- 这条证据把 `InjectedSource` 从“Ghidra XML 旁证”提升成了“DIA COM 直接证据”。
- 同时也把当前 PDB 的 compiland 全景从 `104` 口径推进到更完整的 `106` 口径。
- 本轮进一步新增的 payload 级结论已单独整理在：
  - `DIA_INJECTED_SOURCE_PAYLOAD_2.35.md`
  - `dia_injected_source_2.35.json`
  - `PDB_NAMED_STREAMS_2.35.md`
  - `pdb_named_streams_2.35.json`
  - 其中已经确认：
    - `comtypes` 自动包装只能稳定暴露 `(0, 0)` / `(1, 248)` 这类“长度 + 首字节”现象
    - 完整 104 字节 payload 需要通过原始 vtable 调用提取
    - payload 可稳定解析为 `C# / Microsoft / Text / SHA-256` 四段 GUID 头 + `32` 字节嵌入式 checksum
    - raw PDB named stream map 中的 `19` 个 `/src/files/...` 流与上述 payload 做到 `19/19` 字节级一致
    - `/names` 已可解码为 `19` 对原始/小写路径 + `1` 个空字符串 ID
    - `/src/headerblock` 已可解码为 `19` 条结构化文档元数据 entry，未暴露额外工程资产
