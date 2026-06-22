# 2.36 DIA InjectedSource 与特殊 Compiland 证据

## 结论
- 当前 2.36 shipped classic PDB 的 DIA `EnumTables` 里，明确存在：
  - `InjectedSource`
  - `SourceFiles`
  - `Symbols`
  - `LineNumbers`
  - `InputAssemblyFiles`
- 其中 `InjectedSource` 不是 XML 导出器独有产物，而是 **DIA COM 层直接可枚举** 的真实表。
- 当前量化结果已经升级为：
  - `SourceFiles = 20`
  - `InjectedSource = 20`
  - `LineNumbers = 12844`
  - `Symbols = 10978`
  - `InputAssemblyFiles = 0`
- 全量 `SymTagCompiland` 当前总数是 `109`，比当前 line-span / method-map 口径里的 `107` 多出 2 个特殊 compiland：
  - `* CompilerInfo *`
  - `<DanglingDocuments*223343bd-a859-41a3-90c0-9dfa101f1a95>`
- 这说明 2.36 PDB 的“真实 compiland 全景”当前更准确的口径是：
  - `107` 个源码相关 compiland
  - `2` 个 PDB 内部特殊 compiland

## 方法
- 使用本地 `pydia2` 环境通过 `Dia2Lib` 直接加载：
  - `Intel大小核神经网络调度器N版2.36\IntlThrdSchd\IntlThrdSchd.pdb`
- 当前复用了已验证可通用的脚本：
  - `recovered_src_2.36/recovery_artifacts/scripts/extract-dia-injected-source.py`
- 生成工件：
  - `recovery_artifacts/dia_injected_source_2.36.json`
  - `recovery_artifacts/DIA_INJECTED_SOURCE_PAYLOAD_2.36.md`
  - `recovery_artifacts/VS_TEMPLATE_EVIDENCE_2.36.md`
- 目的：
  - 把 2.35 已验证过的 `InjectedSource` 发现，提升为 2.36 自己的 DIA COM 级证据

## EnumTables 结果
- `SourceFiles = 20`
- `LineNumbers = 12844`
- `InjectedSource = 20`
- `InputAssemblyFiles = 0`
- `Symbols = 10978`

这组结果说明：
- 当前 PDB 中没有 input assembly sidecar 记录
- 但 source file / injected source / symbols / line numbers 四类表都是真实存在的

## InjectedSource 结果
- `InjectedSource.count = 20`
- `SourceFiles.count = 20`
- 两者文件名集合完全一致

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

### 当前 payload 级结论
- `row_count = 20`
- `unique_payload_sha256_count = 20`
- `unique_first_32_hex_count = 1`
- `all_first_32_hex_identical = true`
- 当前 payload 头继续稳定解析为：
  - `C#`
  - `Microsoft`
  - `Text`
  - `SHA256`
- 当前 `recovered_sha256_match_count = 3`
  - 当前已命中的文档：
    - `Program.cs`
    - `Service1.Designer.cs`
    - `obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs`
  - 这说明 2.36 恢复树已经从“0 个文本级命中”推进到至少 `3/20` 个文档可被机器校验为逐字节一致

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
- `VectorMathNew.cs`
- `obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs`

## 特殊 Compiland
- 当前全量 `SymTagCompiland` 数量：
  - `109`
- 当前可见的特殊 compiland 仍只有 2 个：
  - `* CompilerInfo *`
  - `<DanglingDocuments*223343bd-a859-41a3-90c0-9dfa101f1a95>`

## 对恢复树的影响
- 这轮新证据进一步加固了以下结论：
  - 当前源码主文件集合已从 2.35 的 `19` 个文档扩展到 2.36 的 `20` 个文档
  - `VectorMathNew.cs` 已经不只是类型清单里的“新出现类型”，而是 PDB `InjectedSource` 文档集里的真实新增文件
  - `obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs` 继续属于 PDB 中的真实文档集，而不是字符串噪音
  - `Program.cs` 当前已经实现文本级命中，并且其 embedded checksum 与 2.35 当前一致
  - `Service1.Designer.cs` 当前已经实现文本级命中，并且可由本机 Visual Studio 中文 `WindowsService` 模板直接复现
  - `* CompilerInfo *` 与 `<DanglingDocuments*...>` 继续存在，但仍未额外吐出 `.sln` / `.csproj` 这类 VS 元数据

## 当前意义
- 这条证据把 2.36 的 `InjectedSource` 从“字符串 / XML 旁证”提升成了“DIA COM 直接证据”。
- 同时也把 2.36 的 compiland 全景从 `107` 口径推进到更完整的 `109` 口径。
- 后续继续做 2.36 文本级逼近时，可以直接以 `dia_injected_source_2.36.json` 中的嵌入式 `SHA-256` 为 machine-checkable 锚点。
