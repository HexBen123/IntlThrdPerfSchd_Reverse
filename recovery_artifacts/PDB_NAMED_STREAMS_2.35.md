# 2.35 Raw PDB Named Stream 证据

## 结论
- 当前 2.35 shipped classic PDB 已经通过 raw MSF 7.00 目录解析直接确认存在 `23` 个 named stream，而不只是字符串层命中。
- 这 `23` 个 named stream 的构成当前已经明确：
  - `19` 个 `/src/files/...`
  - `1` 个 `/src/headerblock`
  - `1` 个 `/names`
  - `2` 个零长度流：`/LinkInfo`、`/TMCache`
- 最关键的新结论是：
  - `19` 个 `/src/files/...` named stream 都不是源码正文
  - 它们的大小全部都是 `104` 字节
  - 并且与 `dia_injected_source_2.35.json` 中同名文档的 `payload_hex` 做到 `19/19` 字节级完全一致
- 因而当前这份 classic PDB 里，`/src/files/...` 更准确地说是 InjectedSource 元数据记录的 raw named-stream 镜像，不是可以直接还原源码正文的 embedded source 文本流。

## 方法
- 新增脚本：
  - `recovery_artifacts/scripts/extract-pdb-named-streams.py`
- 生成工件：
  - `recovery_artifacts/pdb_named_streams_2.35.json`
- 提取路径：
  - 不依赖 `llvm-pdbutil`、`pdbstr`、`srctool`、`cvdump`、`dia2dump`
  - 直接按 MSF 7.00 superblock、directory stream、PDB stream 1 的 named stream map 做最小解析
  - 再与 `dia_injected_source_2.35.json` 做 payload 级比对

## 当前定量结果
- `superblock.block_size = 512`
- `superblock.num_blocks = 1507`
- `directory.num_streams = 138`
- `pdb_stream.named_stream_map_size = 23`
- `pdb_stream.named_stream_map_capacity = 38`
- `pdb_stream.trailing_tail_u32s = [0, 20140508]`
- `summary.source_file_named_stream_count = 19`
- `summary.zero_length_named_stream_count = 2`
- `summary.source_file_named_stream_unique_sizes = [104]`
- `summary.source_file_named_streams_match_injected_payload_count = 19`
- `summary.all_source_file_named_streams_match_injected_payload = true`

## /names 解码结果
- `/names` 已按 LLVM `PDBStringTable` 结构成功解码。
- 当前关键字段为：
  - `signature = 0xEFFEEFFE`
  - `hash_version = 1`
  - `hash_count = 61`
  - `name_count = 39`
  - `string_count = 38`
  - `empty_string_id_count = 1`
- 当前字符串集合的结构已经收敛得很清楚：
  - `19` 条原始大小写路径
  - `19` 条全小写路径
  - 再加 `1` 个空字符串 ID
- 当前 `/names` 中所有非空字符串都表现为文件路径，没有出现：
  - `.sln`
  - `.csproj`
  - natvis 文件名
  - 其他额外工程元数据字符串
- 这说明 `/names` 不是“还藏着更多工程元信息的杂项流”，而基本就是这 19 个文档的大小写路径表。

## /src/headerblock 解码结果
- `/src/headerblock` 已按 LLVM `InjectedSourceStream` 使用的头结构成功解码。
- 当前关键字段为：
  - `version = 0x0130E21B`
  - `header_size = 924`
  - `age = 1`
  - `table_size = 19`
  - `table_capacity = 38`
- 当前 `19` 条 entry 已全部成功解码，且模式完全一致：
  - `entry_size = 40`
  - `compression = 101`
  - `is_virtual = 0`
  - `object_name` 全部都是空字符串 ID
  - `file_name` 全部是原始大小写路径
  - `virtual_file_name` 全部是对应的全小写路径
  - `key == virtual_file_name_id`
  - `file_size = 104`
- 这里的 `file_size = 104` 不是源码正文长度，而是对应 `/src/files/...` 元数据记录本身的长度；这也和前面 `19` 个 `/src/files/...` 流全部是 `104` 字节完全一致。
- 因而 `/src/headerblock` 当前也没有暴露出额外源码正文、natvis 正文或额外工程资产，只是在更结构化地索引同一批 19 个文档元数据。

## Named Stream 结构
- 零长度 named stream：
  - `stream 5 = /LinkInfo`
  - `stream 6 = /TMCache`
- 非源码 named stream：
  - `stream 7 = /names`，大小 `4412`
  - `stream 8 = /src/headerblock`，大小 `924`
- `/src/files/...` named stream：
  - 当前稳定分布在 `stream 11..29`
  - 没有缺口
  - 全部大小 `104`

## 对 InjectedSource 的新旁证
- 当前 `/src/files/...` 的 raw stream bytes 与 `InjectedSource.get_source(...)` 的 payload 存在更强关系：
  - 不是“相似”
  - 不是“同结构不同内容”
  - 而是 `19/19` 完全相等
- 这意味着：
  - 先前从 DIA 层看到的 `InjectedSource` payload，不是孤立的 COM 表现
  - 这些 `104` 字节记录同时也被 classic PDB 的 named-stream 层直接持有
  - 但它们仍然只是 `C# / Microsoft / Text / SHA256 + checksum` 元数据，而不是源码文本

## 对剩余保真差距的意义
- 这条证据直接收紧了后续工作的边界：
  - 继续深挖 `/src/files/...` 不会直接吐出源码正文
  - 当前更值得投入的是：
    - 继续用嵌入式 `SHA-256` 反推剩余文件的原始文本形态
    - 如果还要继续，只剩模板化小文件的文本级 checksum 命中搜索最有收益
- 同时它也消除了一个高风险误判：
  - 不能再把 `/src/files/...` 当成“可能还没正确解压出来的源码文本流”
  - 也不能再把 `/names` 或 `/src/headerblock` 当成“也许还藏着 `.sln/.csproj/natvis` 正文”的待开发宝藏

## 当前仍未解开的点
- `pdb_stream.trailing_tail_u32s = [0, 20140508]` 中前导 `0` 的精确常量语义
- 其余 `17/19` 未命中文档的原始文本细节
