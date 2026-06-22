# 2.36 本机 Visual Studio 模板命中证据

## 目标
- 验证 2.36 剩余模板化小文件中，是否存在可以直接由本机 Visual Studio 中文模板恢复到文本级 checksum 命中的文件。
- 把“穷举搜索未命中”和“本机模板直接命中”两类结果分开记录，避免后续混淆。

## 本机模板来源
- `vswhere.exe`
  - `C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe`
- 当前实际安装路径：
  - `D:\Microsoft Visual Studio\18\Community`

## 已核对的模板文件
- Windows Service 模板：
  - `D:\Microsoft Visual Studio\18\Community\Common7\IDE\ProjectTemplates\CSharp\Windows\2052\WindowsService\service1.designer.cs`
- Installer ItemTemplate：
  - `D:\Microsoft Visual Studio\18\Community\Common7\IDE\ItemTemplates\CSharp\General\2052\Installer\installer.cs`
  - `D:\Microsoft Visual Studio\18\Community\Common7\IDE\ItemTemplates\CSharp\General\2052\Installer\installer.designer.cs`

## Service1.Designer.cs 命中
- 目标文件：
  - `recovered_src_2.36\IntlThrdPerfSchd\Service1.Designer.cs`
- 目标 embedded checksum：
  - `55ebaa1a84f3b504cbb7d938d18cceca024495279e7904d47ee17c1a20e30b6b`
- 命中方式：
  - 读取本机 `WindowsService\service1.designer.cs` 中文模板
  - 仅做一处文本替换：
    - `$safeprojectname$` -> `IntlThrdPerfSchd`
  - 以 `UTF-8 BOM + CRLF` 形式写回目标文件
- 当前结果：
  - `Service1.Designer.cs` 的 `SHA-256` 已与 embedded checksum 完全一致
  - `dia_injected_source_2.36.json` 中该文件当前为：
    - `recovered_sha256_matches_embedded_checksum = true`

## 当前写回后的原稿形态
- `block-scoped namespace`
- 中文 XML summary 注释
- 中文 Designer region 注释
- `private System.ComponentModel.IContainer components = null;`
- `components = new System.ComponentModel.Container();`
- `this.ServiceName = "Service1";`

## ProjectInstaller 模板直接替换结果
- `installer.cs`
  - 当前直接替换：
    - `$rootnamespace$` -> `IntlThrdPerfSchd`
    - `$safeitemrootname$` -> `ProjectInstaller`
    - 去掉模板条件片段 `$if$...$endif$`
    - 以 `UTF-8 BOM` 写成临时文件后再计算 `SHA-256`
  - 结果 hash：
    - `02537955b9e775a71b98778774cd92cd8e3b53dfa3420bf13b28a43e337c9ebc`
  - 目标 hash：
    - `5eaf364e732ef11197449d06b1c09aa3cf61509b52cc2823b8c2880741455b9d`
  - 结论：
    - 未命中
- `installer.designer.cs`
  - 当前直接替换：
    - `$rootnamespace$` -> `IntlThrdPerfSchd`
    - `$safeitemrootname$` -> `ProjectInstaller`
    - 以 `UTF-8 BOM` 写成临时文件后再计算 `SHA-256`
  - 结果 hash：
    - `9fc15dee6d87fb73f3a92aeebc0b265f20cc50768ce64d36c094f584b67dc384`
  - 目标 hash：
    - `6013fffa85739d2796c81344ef609335bc21701eb69b3a85413c04e238e16140`
  - 结论：
    - 未命中

## VS/DTE 临时样本结果
- 当前脚本：
  - `recovery_artifacts/scripts/generate-vs-dte-projectinstaller-sample.ps1`
- 当前摘要工件：
  - `recovery_artifacts/vs_dte_projectinstaller_sample_2.36.json`
  - `recovery_artifacts/vs_dte_projectinstaller_roundtrip_2.36.json`
- 当前生成链：
  - 用 `cswindowsservice.vstemplate` 创建临时 `IntlThrdPerfSchd` Windows Service 工程
  - 再用 `installer.vstemplate` 向工程插入 `ProjectInstaller.cs`

### DTE 生成的空白 Installer 样本
- `ProjectInstaller.cs`
  - 当前 DTE 实际生成 hash：
    - `ae1e0b260d7f61482007eb98456edff20d63066964c2bfea230890f72430fa6c`
  - 目标 hash：
    - `5eaf364e732ef11197449d06b1c09aa3cf61509b52cc2823b8c2880741455b9d`
  - 结论：
    - 未命中
- `ProjectInstaller.Designer.cs`
  - 当前 DTE 实际生成 hash：
    - `9fc15dee6d87fb73f3a92aeebc0b265f20cc50768ce64d36c094f584b67dc384`
  - 目标 hash：
    - `6013fffa85739d2796c81344ef609335bc21701eb69b3a85413c04e238e16140`
  - 结论：
    - 未命中
- 这说明：
  - 2.36 shipped `ProjectInstaller*` 并不是“直接把 VS 默认 Installer item template 插进工程后不再修改”的结果。

### DTE 对当前恢复稿的 Designer round-trip
- 当前 round-trip 做法：
  - 先生成同一套临时 Windows Service + Installer 样本
  - 再把当前恢复树里的：
    - `recovered_src_2.36\IntlThrdPerfSchd\ProjectInstaller.cs`
    - `recovered_src_2.36\IntlThrdPerfSchd\ProjectInstaller.Designer.cs`
    覆盖到临时工程
  - 然后通过 DTE 打开 `ProjectInstaller.cs` 的设计器视图并执行 `SaveAll`
- 当前 round-trip 后结果：
  - `ProjectInstaller.cs`
    - `19bf4c8dda5769e0b6b2079dbb9d56b25052c6565bbeb6273926be8cab336ed0`
  - `ProjectInstaller.Designer.cs`
    - `1cadf82b4326d144449eec21728e94b973bfeddd1fe6d5be9889a1ffff3e2c33`
- 这两个 hash 与当前恢复树文件完全一致，仍然没有命中 shipped embedded checksum。
- 这说明：
  - 当前恢复稿可以被本机 DTE / Designer 链接受
  - 但一次“无新增设计操作”的 round-trip 不会自动把它们重写成作者原稿文本
  - 因而剩余差异更可能来自：
    - 当年真实设计器操作序列
    - 事件绑定生成链
    - 非默认模板文本层细节
    - 或作者手工编辑后的壳层差异

## 与搜索脚本的关系
- 当前搜索脚本：
  - `recovery_artifacts/scripts/search-designer-checksum-candidates.py`
- 当前搜索结果：
  - `recovery_artifacts/designer_checksum_search_2.36.json`
- 当前结果是：
  - `ProjectInstaller.cs tested_candidate_count = 300`
  - `ProjectInstaller.Designer.cs tested_candidate_count = 12`
  - `Service1.Designer.cs tested_candidate_count = 16908`
  - 三者当时都未命中
- 这说明：
  - 约束搜索可以帮助收窄边界
  - 但 `Service1.Designer.cs` 的最终命中不是靠穷举，而是靠本机 VS 中文模板的直接旁证

## 当前意义
- `Service1.Designer.cs` 已从“sequence-point 强约束 + 未命中的模板搜索”推进到“本机模板直接复现 + InjectedSource checksum 命中”。
- 这类证据强度高于单纯的结构推断，因此当前可把 `Service1.Designer.cs` 视为 2.36 恢复树中的第三个文本级命中文档。
- 后续模板化小文件的继续深挖，应把重心收窄到：
  - `ProjectInstaller.cs`
  - `ProjectInstaller.Designer.cs`
