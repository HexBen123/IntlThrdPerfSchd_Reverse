# 2.36 Raw PDB Named Stream 证据

## 结论
- 当前 2.36 shipped classic PDB 已经通过 raw MSF 7.00 目录解析直接确认存在 `24` 个 named stream，而不只是字符串层命中。
- 这 `24` 个 named stream 的构成当前已经明确：
  - `20` 个 `/src/files/...`
  - `1` 个 `/src/headerblock`
  - `1` 个 `/names`
  - `2` 个零长度流：`/LinkInfo`、`/TMCache`
- 最关键的新结论是：
  - `20` 个 `/src/files/...` named stream 都不是源码正文
  - 它们与 `dia_injected_source_2.36.json` 中同名文档的 `payload_hex` 做到 `20/20` 字节级完全一致
- 因而当前这份 classic PDB 里，`/src/files/...` 更准确地说是 `InjectedSource` 元数据记录的 raw named-stream 镜像，不是可以直接还原源码正文的 embedded source 文本流。

## 方法
- 当前复用了已验证可通用的脚本：
  - `recovered_src_2.35/recovery_artifacts/scripts/extract-pdb-named-streams.py`
- 生成工件：
  - `recovery_artifacts/pdb_named_streams_2.36.json`
- 提取路径：
  - 不依赖 `llvm-pdbutil`、`pdbstr`、`srctool`、`cvdump`、`dia2dump`
  - 直接按 MSF 7.00 superblock、directory stream、PDB stream 1 的 named stream map 做最小解析
  - 再与 `dia_injected_source_2.36.json` 做 payload 级比对

## 直接证据
- `named_stream_count = 24`
- `source_file_named_stream_count = 20`
- `zero_length_named_stream_count = 2`
- `source_file_named_streams_match_injected_payload_count = 20`
- `all_source_file_named_streams_match_injected_payload = true`

### 关键 stream
- `/LinkInfo`
  - `stream_index = 5`
  - `size = 0`
- `/TMCache`
  - `stream_index = 6`
  - `size = 0`
- `/names`
  - `stream_index = 7`
  - `size = 4580`
- `/src/headerblock`
  - `stream_index = 8`
  - `size = 968`

## `/names` 结果
- `name_count = 41`
- `string_count = 40`
- `original_case_path_count = 20`
- `lowercase_path_count = 20`
- `empty_string_id_count = 1`

这说明：
- `/names` 仍然可以解成标准 `PDBStringTable`
- 当前只包含：
  - `20` 条原始大小写路径
  - `20` 条全小写路径
  - `1` 个空字符串 ID
- 当前没有额外 `.sln`、`.csproj`、natvis 或其他工程资产名称

## `/src/headerblock` 结果
- `entries_count = 20`
- `all_entries_have_size_40 = true`
- `all_entries_compression_101 = true`
- `all_entries_not_virtual = true`

这说明：
- `/src/headerblock` 当前只是更结构化地索引同一批 `20` 个源码文档元数据
- 它没有把额外 VS 工程资产或源码正文藏在别处

## 对 2.36 恢复的影响
- 当前 2.36 的 PDB 内部结构与 2.35 明显同型，只是文档数从 `19` 扩展到了 `20`
- 新增文档集与 `InjectedSource` 集合、PDB 字符串路径集合、当前恢复树文件集合互相闭环
- 当前 package 内已经没有证据支持“还藏着没解出来的源码正文流”或“还藏着 `.sln/.csproj` 正文”

## 当前意义
- 这条证据把 2.36 的 PDB 旁证链推进到了 raw named-stream 级。
- 同时也进一步收紧了工程元数据恢复边界：
  - 当前可以高置信恢复 project root、主文件集合、Designer 资源命名和文档元数据结构
  - 但仍不足以直接恢复原始 `.sln` 名、project GUID 或 solution 配置矩阵
