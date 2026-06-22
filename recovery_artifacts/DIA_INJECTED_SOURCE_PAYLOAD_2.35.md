# 2.35 DIA InjectedSource Payload 内容级证据

## 结论
- 当前 `IDiaInjectedSource.get_source(...)` 的 payload 已经不再是“未知二进制块”，而是可稳定提取、可结构化解释的 **104 字节调试元数据记录**。
- `comtypes` 自动包装层并不能正确把 `pbData` 当作整块缓冲区返回：
  - `get_source(0)` 当前稳定返回 `(0, 0)`
  - `get_source(1)` 当前稳定返回 `(1, 248)`
  - 其中第二个值 `248` 实际上只是首字节 `0xF8` 被错误解组，不是完整 buffer 指针，也不是完整 payload
- 直接走 `IDiaInjectedSource` 的原始 vtable 调用后，当前 19 条 payload 全都能稳定读出。
- 当前 payload 的前 64 字节可稳定解析为 4 个 GUID：
  - `3F5162F8-07C6-11D3-9053-00C04FA302A1`
    - `CorSym_LanguageType_CSharp`
  - `994B45C4-E6E9-11D2-903F-00C04FA302A1`
    - `CorSym_LanguageVendor_Microsoft`
  - `5A869D0B-6611-11D3-BD2A-0000F80849BD`
    - `CorSym_DocumentType_Text`
  - `8829D00F-11B8-4213-878B-770E8597AC16`
    - `SHA256`
- 当前 payload 的后半段稳定表现为：
  - `checksum_length = 32`
  - `reserved_dword = 0`
  - 随后跟随 32 字节嵌入式 SHA-256
- 因此这 19 条 `InjectedSource` 当前已经可以被解释为：
  - `C#`
  - `Microsoft`
  - `Text`
  - `SHA-256 checksum`
  - 再加每个文档各自不同的 32 字节哈希

## 方法
- 新增脚本：
  - `recovery_artifacts/scripts/extract-dia-injected-source.py`
- 生成工件：
  - `recovery_artifacts/dia_injected_source_2.35.json`
- 提取策略：
  - 保留 `comtypes` 层 `cbData=0/1` 的包装层观测，证明它只暴露了“长度 + 首字节”
  - 完整 payload 统一改为直接调用 `IDiaInjectedSource` 原始 vtable slot `9`
  - 对每条 payload 解析：
    - 前 4 个 little-endian GUID
    - `checksum_length`
    - `reserved_dword`
    - `embedded_checksum_hex`
  - 同时把当前恢复树里同名文件的 `SHA-256` 一并做对比

## 当前定量结果
- `row_count = 19`
- `unique_payload_sha256_count = 19`
- `unique_first_32_hex_count = 1`
- `all_first_32_hex_identical = true`
- `recovered_sha256_match_count = 2`

这说明：
- 19 条记录共享同一组语言/厂商/文档类型/哈希算法头
- 但每条记录自己的嵌入式 `SHA-256` 都不同
- 当前恢复树里已经有 2 个文档做到了与 payload 内嵌 checksum 的字节级一致：
  - `Program.cs`
  - `obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs`
- 其余 `17` 个源码文件当前仍是**高保真语义恢复**，但不是**原始文本字节级复刻**

## Raw Named Stream 旁证
- 当前又新增了一条独立于 DIA COM 的 raw PDB 旁证，见：
  - `PDB_NAMED_STREAMS_2.35.md`
  - `pdb_named_streams_2.35.json`
- 该路径直接解析 classic PDB 的 MSF 目录和 stream 1 named stream map 后，当前确认：
  - 共有 `23` 个 named stream
  - 其中 `19` 个 `/src/files/...` named stream 全部大小都是 `104`
  - 这 `19` 个 raw named stream 与 `InjectedSource` 的 `payload_hex` 做到 `19/19` 字节级完全一致
- 这进一步说明：
  - `InjectedSource` payload 不是 DIA 层私有解释结果
  - `/src/files/...` 也不是“还没被正确解压出来的源码正文”
  - 它们本身就是同一批 `104` 字节源码校验元数据记录
  - 进一步解码 `/names` 与 `/src/headerblock` 后也已确认：
    - `/names` 只含 `19` 对原始/小写路径再加 `1` 个空字符串 ID
    - `/src/headerblock` 只是在结构化索引这 `19` 个文档元数据
    - 当前没有任何额外 `.sln/.csproj/natvis` 正文被藏在这两条流里

## 示例 1
- `Program.cs` 当前已经实现了文本级命中：
  - `embedded_checksum_hex = 150e8b212eb7869cfc594975820bb3efcd9beeef6e39551c0dad4da75c23483e`
  - 当前恢复树中的 `Program.cs` 已经调整为：
    - `UTF-8 BOM`
    - `CRLF`
    - 6 条旧式默认 `using`
    - 中文 XML summary 注释
    - `ServicesToRun = new ServiceBase[] { ... }` 的标准服务模板写法
  - 当前工作树中文件的 `SHA-256` 已与 payload 内嵌 checksum 完全一致

## 示例 2
- `RealtimeScheduler.cs` 的 payload 当前可解析为：
  - `language = CorSym_LanguageType_CSharp`
  - `vendor = CorSym_LanguageVendor_Microsoft`
  - `document_type = CorSym_DocumentType_Text`
  - `checksum_algorithm = SHA256`
  - `checksum_length = 32`
  - `embedded_checksum_hex = 9420792e6edba9bb523e85a90f6370d545403080e1110f9c3b146bea1c0dfb09`
- 当前恢复树中的：
  - `recovered_src_2.35\IntlThrdPerfSchd\RealtimeScheduler.cs`
  - `SHA-256 = 904267e130773b9ece0bbbeddabe1dd7221c53b41aeb66f0a9b374b4d0978339`
- 两者不一致，说明当前树在该文件上仍然不是字节级原稿。

## 对保真评估的意义
- 这轮推进解决的不是“工程树还缺哪些文件”，而是把 `InjectedSource` 从“表级存在性证据”推进到了“内容级 checksum 证据”。
- 当前这套恢复树依然可视为**接近原作者工程结构与语义**的高保真工程树。
- 但如果按“逐文件文本字节级一致”这个更严格口径衡量，当前仍只有 `2/19` 个 `InjectedSource` 文档命中了已恢复树。
- 因而这份 payload 证据更适合被当作：
  - 原始源码文本级别的校验锚点
  - 后续继续逼近 100% 时的 machine-checkable 基准

## 当前仍不能直接推出的点
- payload 中 `reserved_dword = 0` 的具体语义
- 为什么 classic PDB 这里以 `InjectedSource` / `/src/files/...` 双重形式承载了文档 hash 记录，而不是更直观的“源码正文”
- 原始 `.sln`
- 原始 `project GUID / solution GUID`
