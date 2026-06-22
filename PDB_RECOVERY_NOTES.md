# Intel 大小核神经网络调度器 N 版 2.51 调试信息/元数据恢复说明

本文件名沿用 `PDB_RECOVERY_NOTES.md`，但对 2.51 而言它记录的重点是：2.51 **缺失可用的 PDB 直接证据**，因此恢复树只能以“结构模板 + 语义同步 + 可审计工件”为主。

## 1. 直接结论（最重要）
- 2.51 发布包目录内未发现 `IntlThrdSchd.pdb`。
- 对 `IntlThrdSchd.exe` 做二进制字符串与调试目录签名扫描后：
  - 未发现 `RSDS`/`NB10` CodeView 签名
  - 未发现 `.pdb` 路径或 `.cs/.sln/.csproj` 相关字符串
- 因此：无法像 2.35/2.36 那样用 `PDB + DIA` 做 compiland 级归位、sequence point、InjectedSource checksum 等“强证据链”恢复。

对应可审计证据已固化：
- `recovery_artifacts/binary_string_scan_2.51.txt`
- `recovery_artifacts/generate_binary_string_scan_2.51.py`

## 2. 仍可利用的直接信号
即便没有 PDB，2.51 的 `IntlThrdSchd.exe` 仍暴露了少量可用字符串：
- `IntlThrdSchd` / `IntlThrdPerfSchd`
- `IntlThrdSchd.ProjectInstaller.resources`
- `IntlThrdSchedErrorInfo.txt`（UTF-16LE，说明运行时可能会落盘错误信息）

这些信号用于支撑：
- 程序集/资源层命名：保持 `AssemblyName/RootNamespace = IntlThrdSchd`
- 源码树/项目层命名：继续沿用历史版本更常见的 `IntlThrdPerfSchd` 结构

## 3. 本轮恢复策略（2.51）
在缺失 PDB 的前提下，当前恢复采用以下策略：
- 以 2.36 的“已验证作者式组合文件边界”作为模板。
- 以 2.51 的 ILSpy/dnSpy 导出作为实现真值来源，把实现同步进模板化文件边界。
- 用“实体清单对比”做一致性验收：确保 public/内部类型基本齐全，差异收敛到可解释范围。

对应工件：
- `_dnspy_export/`、`_ilspy_export/`、`_ilspy_export_cs8/`
- `recovery_artifacts/entities_original_2.51.txt`
- `recovery_artifacts/entities_recovered_2.51.txt`
- `recovery_artifacts/entities_diff_2.51.txt`

## 4. 当前已完成项
- 恢复出可构建的 `net48` 工程骨架：`IntlThrdPerfSchd.sln` + `IntlThrdPerfSchd/IntlThrdPerfSchd.csproj`
- 已按模板完成“组合文件”归位与 partial/Designer 边界拆分（详见 `README.md`）。
- 已完成“原始 2.51 vs 本地构建产物”的实体清单对比：
  - 当前差异只剩 1 处编译器生成闭包 DisplayClass 编号漂移（属于编译器/方法顺序差异）。

## 5. 仍需继续深挖的点（后续路线）
- 在没有 PDB 的情况下，进一步逼近“原作者项目结构”的主要抓手是：
  - 找到更多能支撑“文件命名/目录结构”的直接信号（例如：日志文件名、配置键名、资源命名、命令行参数等）。
  - 把 `Service1` 等大文件的逻辑块进一步拆回更接近作者的文件边界（如果后续发现新证据能支撑）。
  - 如果能拿到同版本或相近版本的 PDB（或作者工程残留），再把 2.51 恢复树提升到 PDB/DIA 级别的可校验形态。