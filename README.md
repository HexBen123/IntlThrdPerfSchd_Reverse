# Intel 大小核神经网络调度器 N 版 2.81 源码主线恢复树

本目录包含基于 `Intel大小核神经网络调度器N版2.81永久权重全功能版` 发布包恢复出的 C# 工程树。最终目标设定为作者原项目真源码 `100%` 还原；当前主线要求 `IntlThrdPerfSchd` 源码项目逐步逼近作者原始工程结构和源码表达，并保持普通 `dotnet build` 可用。二进制 IL 对齐报告只作为审计证据，不替代源码恢复本身。

## 当前进度

- 源码主线恢复进度估算：`97% / 100%`
- 稳定方法 IL hash 对齐：`1902 / 1923 = 98.908%`
- 实体数量：`194 / 194`
- manifest resource：字节级一致
- metadata surface 数量：`11909 / 11909`
- 普通 Release 构建：通过，`180 warnings, 0 errors`

这个百分比不是“已经拿到作者原始仓库”的证明，而是当前恢复树相对原始 EXE 的结构、资源、实体、构建和方法 IL 证据综合估算。最终目标是 `100%` 作者真源码；剩余差距主要来自 21 个方法的 IL 形态差异、若干编译器生成 lambda/local-function 成员编号漂移，以及 `Service1` 大文件边界仍需继续精修。

## 原始基线

- 原始主程序：
  - `Intel大小核神经网络调度器N版2.81永久权重全功能版/IntlThrdSchd/IntlThrdSchd.exe`
  - 文件大小：`389120`
  - SHA256：`53037500DD638987BA781939DEF20DCD9B444746B91F0B874B3EF0695D1CCEA6`
  - `FileVersion/ProductVersion`: `0.0.0.0`
- 恢复树内置基线：
  - `recovery_artifacts/original/IntlThrdSchd.exe`
- 2.81 发布包未在初始盘点中发现匹配 PDB，所以本轮没有 PDB 强真值路径；恢复主线以 ILSpy/dnSpy 导出、原始 EXE 二进制审计和旧版本恢复树对照为证据。

## 目录说明

- `IntlThrdPerfSchd.sln`
  - 恢复出的解决方案入口。
- `IntlThrdPerfSchd/`
  - 主源码工程，目标框架为 `net48`。
  - `AssemblyName` 保持 `IntlThrdSchd`。
  - `RootNamespace` 为 `IntlThrdSchd`，用于匹配原始 EXE 的 manifest resource 名称。
  - `lib/` 保存构建所需的直接引用 DLL。
  - `ProjectInstaller` 已拆为 `ProjectInstaller.cs` 与 `ProjectInstaller.Designer.cs`，匹配 Windows 服务项目常见 designer 边界。
- `shipped_payload/`
  - 从 2.81 发布包保留的运行和安装 payload，包括 WinRing0 文件、TraceEvent/DIA 相关 DLL、系统依赖 DLL、安装脚本和注册表文件。
- `_ilspy_export/`
  - ILSpy 默认语言版本项目导出。
- `_ilspy_export_cs8/`
  - ILSpy C# 8 项目导出，是当前源码主线的主要来源。
- `_dnspy_export/`
  - dnSpy 导出的反编译旁证，保留了另一套文件边界和 token 注释视角。
- `recovery_artifacts/`
  - 原始 EXE、实体清单、resource 对比、metadata surface 对比、方法 IL hash 对比和源码恢复审计记录。
  - `generate_entity_lists_2.81.ps1`、`compare_manifest_resources_2.81.ps1`、`compare_method_il_hashes_2.81.ps1`、`compare_metadata_surface_2.81.ps1` 可用默认路径复现当前报告。

## 构建方式

使用 PowerShell 7 在仓库根目录运行：

```powershell
dotnet build .\recovered_src_2.81\IntlThrdPerfSchd.sln -c Release
```

当前验证结果：

- `180 warnings`
- `0 errors`

这些警告主要来自反编译源码中保留下来的未使用字段、未使用局部变量和编译器重建痕迹。它们不是当前恢复树的构建错误，但也是后续源码精修时需要逐项用证据判断的区域。

## 审计证据

### Manifest resource

报告：`recovery_artifacts/manifest_resource_compare_2.81.txt`

- resource：`IntlThrdSchd.ProjectInstaller.resources`
- original：`len=180 sha256=E13ED2C59366D0EEA74863FD71A81F0CB977CCE1EDFDE304FC538690A4F6AC89`
- recovered：`len=180 sha256=E13ED2C59366D0EEA74863FD71A81F0CB977CCE1EDFDE304FC538690A4F6AC89`
- `equal: True`

### Entity list

脚本：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\recovered_src_2.81\recovery_artifacts\generate_entity_lists_2.81.ps1
```

报告：

- `recovery_artifacts/entities_original_2.81.txt`
- `recovery_artifacts/entities_recovered_2.81.txt`
- `recovery_artifacts/entities_diff_2.81.txt`

当前实体数量均为 `194`。差异仅显示 `OnlineLearningManager` 的一个编译器生成 display class 编号从原始 `<>c__DisplayClass152_0` 漂移为恢复构建的 `<>c__DisplayClass153_0`。

### Method IL hash

脚本：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\recovered_src_2.81\recovery_artifacts\compare_method_il_hashes_2.81.ps1
```

报告：`recovery_artifacts/method_il_hash_diff_2.81.txt`

- `stable_method_count_original: 1923`
- `stable_method_count_recovered: 1923`
- `stable_method_count_compared(intersection): 1923`
- `stable_method_hash_match_count: 1902`
- `stable_method_hash_match_percent: 98.908`
- `missing_in_recovered_count: 0`
- `extra_in_recovered_count: 0`
- `il_mismatch_count: 21`

### Metadata surface

脚本：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\recovered_src_2.81\recovery_artifacts\compare_metadata_surface_2.81.ps1
```

报告：`recovery_artifacts/metadata_surface_diff_2.81.txt`

- `metadata_surface_count_original: 11909`
- `metadata_surface_count_recovered: 11909`
- `metadata_surface_missing_in_recovered_count: 44`
- `metadata_surface_extra_in_recovered_count: 44`

这些差异主要是编译器生成成员和 lambda/local-function 编号漂移，例如 `Service1::<OnStart>b__743_*` 与恢复构建中的 `Service1::<OnStart>b__744_*`，以及部分 `<>c` 缓存字段编号差异。公开源码结构和实体数量已经对齐。

## 已知剩余差距

- 21 个稳定方法的 IL hash 仍未对齐，集中在：
  - `Service1.OnStart`
  - `Service1.UpdateNode*`
  - `Service1.ProcessCompare*`
  - `OnlineLearningManager`
  - `TransformerScheduler`
  - `Service1.CrossAttentionScheduler`
  - `Service1.SchedulerDataset` / `ThreadClassifier`
  - `Tracker` / `Tracker4lat`
- 2026-07-02 的剩余差异复查已把 21 个 mismatch 归类：主要是旧编译器 `callvirt` 与当前编译器 `call`、ValueTuple 取字段降级、`beq` 与 `ceq/brtrue` 分支降级、lambda/display-class 缓存字段编号、局部变量槽位顺序、以及原始 IL 中的 discard-only `pop`。这些点目前没有保留强行凑 IL 的源码改动。
- `Service1.cs` 仍然保留大量反编译字段和局部变量警告，需要后续按方法证据精修，不能为了消警告直接删除。
- 当前没有 2.81 匹配 PDB，因此无法用 PDB sequence points 或原始源码路径进一步证明作者文件边界。

## 后续冲刺 100% 的方向

1. 对 `method_il_hash_diff_2.81.txt` 中剩余 21 个方法继续寻找新的外部证据，例如匹配 PDB、作者源码片段、或能改变编译器降级策略且不污染源码的项目级证据。
2. 继续拆分 `Service1` 的 designer/主逻辑边界，参考 1.26 与 2.51，但只在 2.81 证据支持时落地。
3. 对 lambda/local-function 造成的 compiler-generated 编号漂移做针对性源码调整，减少 metadata surface 差异。
4. 如需要二进制审计产物，可另建 patched EXE 路径，把剩余 mismatch 方法的 IL body 从原始 EXE 移植到恢复构建中；该路径只能作为审计工具，不能作为源码主线完成证明。
5. 只有在源码结构、方法行为、metadata surface、资源、构建输出和可解释的作者式文件边界都能被证据支撑时，才能把进度从阶段性百分比提升到 `100%`。
