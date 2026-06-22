# Intel 大小核调度器 S 版 1.26 源码主线恢复树

本目录包含基于 `intel大小核调度器S版1.26 for 12代及以上` 发布包恢复出的自包含工程树。当前恢复目标是让 `IntlThrdPerfSchd` 源码项目本身尽量贴近作者原始源码与工程结构，普通 `dotnet build` 输出是二次开发和维护的主产物；高保真 patched EXE 只作为原始 IL 对照和审计工具保留。

## 关键边界

- 原始主程序：
  - `intel大小核调度器S版1.26 for 12代及以上/IntlThrdSchd/IntlThrdSchd.exe`
  - SHA256：`E9234BFF224B9F0B21212EB725A5E4E099C4AAFCF113191D842BB3737D40CC47`
  - 文件大小：`73216`
  - `FileVersion/ProductVersion`: `1.0.0.0`
- 随包 PDB 不能作为 1.26 EXE 的源码级强真值：
  - 随包 PDB GUID：`5b52adeb-2014-4350-aa0f-f67d25580bd8`
  - 1.26 EXE CodeView GUID：`9b0b3b48-ce6a-4e38-aa44-af9c9c507fc7`
  - Age：`1`
  - 全仓库扫描到 `26` 个 EXE、`36` 个 PDB，未发现与该 EXE GUID/Age 完全匹配的 PDB。
- 因此当前恢复策略是：
  - 以真实 1.26 EXE 的 ILSpy C# 8 反编译结果作为实现基线；
  - 用 dnSpy 导出、历史高保真恢复树和 DIA/PDB 旁证校正文件边界与命名；
  - 对 C# 编译器难以稳定复刻的 8 个方法，优先恢复自然、可维护、可编译的源码表达；
  - `build-highfidelity.ps1` 仍可用 dnlib 从原始 1.26 EXE 精确移植 IL body 到 patched 产物，但该产物只用于审计和对照，不代表源码主线的完成标准。

## 目录说明

- `IntlThrdPerfSchd.sln`
  - 恢复出的解决方案入口。
- `build-highfidelity.ps1`
  - 一键高保真审计入口：Release 构建、IL body 移植、方法 IL/资源/实体/元数据/文件属性验证。它不是普通开发的推荐构建入口。
- `IntlThrdPerfSchd/`
  - 主要源码工程，目标框架为 `net48`。
  - `AssemblyName` 保持 `IntlThrdSchd`。
  - `RootNamespace` 为 `IntlThrdPerfSchd`，用于匹配原始 EXE 的 manifest resource 名。
  - `lib/` 保存恢复工程自身使用的编译和运行 DLL，不再从原始发布目录解析引用。
- `shipped_payload/`
  - 从 1.26 发布包保留的运行和安装随包 payload，包括 WinRing0 文件、TraceEvent 架构目录、安装脚本和注册表文件。
- `_ilspy_export_nopdb/`
  - ILSpy 无 PDB 项目导出。
- `_ilspy_export_cs8_nopdb/`
  - ILSpy 强制 C# 8 的无 PDB 项目导出。
- `_dnspy_export/`
  - dnSpy 导出的反编译旁证。
- `recovery_artifacts/`
  - 恢复证据、脚本和对比报告。
- `recovery_artifacts/source_fidelity_audit_1.26.md`
  - 8 个历史 IL patch 目标的方法级源码审计记录，说明证据来源、源码决策和剩余边界。
- `recovery_artifacts/original/IntlThrdSchd.exe`
  - 内置的原始 1.26 EXE 基线，供 IL body 移植和一致性验证使用。
- `recovery_artifacts/tools/dnlib.dll`
  - 内置的 dnlib 依赖，供补丁和元数据/IL 对比脚本使用。
- `recovery_artifacts/patched/IntlThrdSchd.exe`
  - 审计用 patched 产物。它不是原始 EXE 的逐字节拷贝，也不是普通源码主线的主产物，而是由恢复源码 Release 构建后，再把剩余差异方法的 IL body 从原始 EXE 精确移植得到。

## 已恢复的作者式文件边界

- `OpenLibSys/Ols.cs` 已归位为 `OpenLibSys.cs`。
- `Service1` 已拆分为：
  - `Service1.cs`
  - `Service1.Designer.cs`
- `ProjectInstaller` 已拆分为：
  - `ProjectInstaller.cs`
  - `ProjectInstaller.Designer.cs`

## 构建方式

### 普通开发构建

使用 PowerShell 7 运行：

```powershell
dotnet build .\recovered_src_1.26\IntlThrdPerfSchd.sln -c Release
```

该命令生成普通 Release 输出，适合源码二次开发和调试。工程引用已改为 `IntlThrdPerfSchd/lib/`，运行和安装 payload 会从 `shipped_payload/` 复制到 `bin/Release/net48/`。

当前验证结果：

- `129 warnings`
- `0 errors`

这些警告主要来自反编译源码中与原始程序一致的大量未使用字段，不是本轮恢复新增的构建错误。

### 一键高保真审计

使用 PowerShell 7 运行：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\recovered_src_1.26\build-highfidelity.ps1
```

该脚本会依次执行：

1. `dotnet build .\recovered_src_1.26\IntlThrdPerfSchd.sln -c Release`
2. `recovery_artifacts/patch_method_bodies_1.26.ps1`
3. 方法 IL hash 对比，包括编译器生成成员
4. manifest resource 字节对比
5. 实体清单对比
6. 元数据表面对比
7. 文件大小、版本字段和输出 payload 检查

审计用 patched 产物仍是：

```text
recovered_src_1.26/recovery_artifacts/patched/IntlThrdSchd.exe
```

注意：patched EXE 是“恢复源码 Release 构建 + 原始 1.26 EXE 的 8 个方法 IL body 精确移植”得到的产物，不是把原始 EXE 逐字节复制到恢复树，也不应作为后续二次开发的源码完成证明。

## 高保真审计证据

以下证据说明审计用 patched EXE 可以继续对照原始 1.26 IL。源码主线的完成边界仍以普通构建、源码自然度和方法级审计记录为准。

### 全方法 IL hash

脚本：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\recovered_src_1.26\recovery_artifacts\compare_method_il_hashes_1.26.ps1 `
  -RecoveredExe .\recovered_src_1.26\recovery_artifacts\patched\IntlThrdSchd.exe `
  -OutPath .\recovered_src_1.26\recovery_artifacts\patched\method_il_hash_diff_1.26_patched_all.txt `
  -IncludeCompilerGenerated
```

报告：

- `recovery_artifacts/patched/method_il_hash_diff_1.26_patched_all.txt`
- `include_compiler_generated: True`
- `stable_method_count_original: 171`
- `stable_method_count_recovered: 171`
- `stable_method_hash_match_count: 171`
- `stable_method_hash_match_percent: 100`
- `missing_in_recovered_count: 0`
- `extra_in_recovered_count: 0`
- `il_mismatch_count: 0`

### Manifest resource

脚本：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\recovered_src_1.26\recovery_artifacts\compare_manifest_resources_1.26.ps1
```

报告：

- `recovery_artifacts/patched/manifest_resource_compare_1.26_patched.txt`
- resource：`IntlThrdPerfSchd.ProjectInstaller.resources`
- original：`len=180 sha256=E13ED2C59366D0EEA74863FD71A81F0CB977CCE1EDFDE304FC538690A4F6AC89`
- recovered：`len=180 sha256=E13ED2C59366D0EEA74863FD71A81F0CB977CCE1EDFDE304FC538690A4F6AC89`
- `equal: True`

### 实体清单

脚本：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\recovered_src_1.26\recovery_artifacts\generate_entity_lists_1.26.ps1
```

报告：

- `recovery_artifacts/entities_original_1.26.txt`
- `recovery_artifacts/entities_patched_1.26.txt`
- `recovery_artifacts/entities_diff_patched_1.26.txt`

当前 `entities_diff_patched_1.26.txt` 为空差异输出。

### 元数据表面

脚本：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\recovered_src_1.26\recovery_artifacts\compare_metadata_surface_1.26.ps1
```

报告：

- `recovery_artifacts/patched/metadata_surface_diff_1.26_patched.txt`
- `metadata_surface_count_original: 2658`
- `metadata_surface_count_recovered: 2658`
- `metadata_surface_missing_in_recovered_count: 0`
- `metadata_surface_extra_in_recovered_count: 0`

该检查覆盖 AssemblyRef、ModuleRef、manifest resource、类型/字段/方法/属性/事件签名、成员属性、参数名和 custom attribute。

### 文件版本与大小

审计用 patched EXE：

- `recovery_artifacts/patched/IntlThrdSchd.exe`
- 文件大小：`73216`，与原始 EXE 一致。
- `FileVersion/ProductVersion`: `1.0.0.0`，与原始 EXE 一致。
- `FileDescription/ProductName`: `IntlThrdSchd`，与原始 EXE 一致。
- SHA256 与原始 EXE 不同，这是预期的：该产物是重建+IL body 精确移植，不是原始文件复制。

## 手动复现审计产物

先构建 Release：

```powershell
dotnet build .\recovered_src_1.26\IntlThrdPerfSchd.sln -c Release
```

再生成审计用 patched EXE：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\recovered_src_1.26\recovery_artifacts\patch_method_bodies_1.26.ps1
```

注意：这些脚本必须使用 PowerShell 7 的 `pwsh` 运行；不要用 Windows PowerShell 5.1 的 `powershell`，否则 UTF-8 路径可能被错误解码。
