# 2.36 DIA InjectedSource Payload 与文本级校验

## 目标
- 把 2.36 的 `InjectedSource` 从“表级存在性证据”继续推进到“源码文本级 checksum 证据”。
- 明确当前恢复树里哪些文件已经做到逐字节命中，哪些仍然只是高保真语义恢复。

## 输入工件
- `recovery_artifacts/scripts/extract-dia-injected-source.py`
- `recovery_artifacts/dia_injected_source_2.36.json`

## 当前定量结果
- `row_count = 20`
- `unique_payload_sha256_count = 20`
- `unique_first_32_hex_count = 1`
- `all_first_32_hex_identical = true`
- `recovered_sha256_match_count = 3`

这说明：
- 20 条记录共享同一组语言/厂商/文档类型/哈希算法头
- 但每条记录自己的嵌入式 `SHA-256` 都不同
- 当前恢复树里已经有 3 个文档做到了与 payload 内嵌 checksum 的字节级一致：
  - `Program.cs`
  - `Service1.Designer.cs`
  - `obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs`

## Program.cs 的当前结论
- 2.36 的 `Program.cs` 当前 embedded checksum 为：
  - `150e8b212eb7869cfc594975820bb3efcd9beeef6e39551c0dad4da75c23483e`
- 它与 2.35 已确认命中的 `Program.cs` checksum 完全一致。
- 当前 2.36 恢复树中的 [Program.cs](G:/BaiduNetdiskDownload/逆向/recovered_src_2.36/IntlThrdPerfSchd/Program.cs) 已调整为与 2.35 相同的字节级文本形态：
  - `UTF-8 BOM`
  - `CRLF`
  - `6` 条旧式默认 `using`
  - 中文 XML summary 注释
  - `ServicesToRun = new ServiceBase[] { ... }` 的标准 Windows Service 模板写法
- 当前工作树中文件的 `SHA-256` 已与 payload 内嵌 checksum 完全一致。

## Release 生成文档锚点
- 当前 `obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs` 的 embedded checksum 为：
  - `af4c24efdd16c0cb3946e8e148fb6df4fc9c501c71cc718738b4728808737373`
- 串行执行：
  - `dotnet build .\recovered_src_2.36\IntlThrdPerfSchd\IntlThrdPerfSchd.csproj -c Release -v minimal -m:1`
- 之后该生成文件也达成了字节级一致。

## Service1.Designer.cs 的当前结论
- 2.36 的 `Service1.Designer.cs` 当前 embedded checksum 为：
  - `55ebaa1a84f3b504cbb7d938d18cceca024495279e7904d47ee17c1a20e30b6b`
- 当前恢复树中的 [Service1.Designer.cs](G:/BaiduNetdiskDownload/逆向/recovered_src_2.36/IntlThrdPerfSchd/Service1.Designer.cs) 已调整为本机 Visual Studio 中文 `WindowsService` 模板的精确文本形态：
  - 模板路径：
    - `D:\Microsoft Visual Studio\18\Community\Common7\IDE\ProjectTemplates\CSharp\Windows\2052\WindowsService\service1.designer.cs`
  - 当前只做了一处替换：
    - `$safeprojectname$ -> IntlThrdPerfSchd`
  - 当前写回口径：
    - `UTF-8 BOM`
    - `CRLF`
    - 中文 XML summary 注释
    - 中文 Designer region 注释
    - `this.ServiceName = "Service1";`
- 当前工作树中文件的 `SHA-256` 已与 payload 内嵌 checksum 完全一致。

## 对保真评估的意义
- 这轮推进解决的不是“工程树还缺哪些文件”，而是把 2.36 的 `InjectedSource` 从 DIA 表证据推进到了 machine-checkable 的文本级锚点。
- 当前恢复树依然主要是**高保真工程结构与源码语义恢复树**。
- 但现在它已经不再是“0 个文件文本级命中”，而是至少有 `3/20` 个文档可被直接机器验证为逐字节一致。

## 当前仍不能直接推出的点
- 其余 `17` 个源码文件当前还没有做到 payload checksum 命中。
- `InjectedSource` payload 仍然不能直接吐出源码正文，它只提供可靠的文本级校验锚点。
- 原始 `.sln` / `project GUID` / `solution GUID` 仍然不在这条证据链里。

## 后续建议
- 继续做 2.36 的文本级逼近时，优先顺序应是：
  - `ProjectInstaller.cs`
  - `ProjectInstaller.Designer.cs`
  - `OnStart` 尾部 energy/TAT 更新块附近的文本级恢复
