<p align="center">
  <h1 align="center">IntlThrdPerfSchd — Reverse Recovery</h1>
  <p align="center">
    <strong>Intel 大小核线程调度器</strong> · 逆向恢复源代码归档
  </p>
</p>

---

## 这是什么？

本仓库归档了 **Intel 大小核线程调度器** 的逆向恢复源代码。原始程序是一个基于 .NET Framework 4.8 的 Windows 服务，利用 Transformer 神经网络模型在线学习线程行为，动态将线程分配到 Intel 12 代及更新处理器的 P-core（性能核）或 E-core（能效核）上，以优化系统整体性能与能效。

每次发布包均为**单一 `IntlThrdSchd.exe`**，不附带源码。本仓库通过 ILSpy / dnSpy 反编译，结合 shipped PDB / DIA / PDB named stream 等旁证，将各个发布版本逐一恢复为**可编译、可维护、贴近原作者工程结构**的 Visual Studio 解决方案。

---

## 版本总览

`main` 分支仅包含本 README。每个恢复版本驻留在独立分支上，分支根目录内容对应原始提交目录中对应版本文件夹的完整恢复产物。

| 分支 | 系列 | 版本 | 架构特点 | 关键文件 |
| --- | --- | --- | --- | --- |
| [`s_1.26`](../../tree/s_1.26) | **S 版** (调度器) | 1.26 | 基础调度器，无神经网络 | `IntlThrdPerfSchd.sln` |
| [`n_2.35`](../../tree/n_2.35) | **N 版** (神经网络) | 2.35 | 初代 Transformer 调度器 | `IntlThrdPerfSchd.sln` |
| [`n_2.36`](../../tree/n_2.36) | **N 版** (神经网络) | 2.36 | 新增 `CoreTransformerEncoder` / `ThreadTransformerEncoder` / `VectorMathNew` | `IntlThrdPerfSchd.sln` |
| [`n_2.51`](../../tree/n_2.51) | **N 版** (神经网络) | 2.51 | 永久权重部署版，无 PDB | `IntlThrdPerfSchd.sln` |

### 系列说明

| 系列 | 代号 | 调度策略 | PDB 可用 |
| --- | --- | --- | --- |
| **S** | 调度器版 | 基于规则的启发式调度 | 随包 PDB（GUID 不匹配，仅旁证） |
| **N** | 神经网络版 | Transformer 在线强化学习调度 | 2.35 / 2.36 有匹配 PDB；2.51 无 PDB |

---

## 仓库结构

```
main 分支（当前分支）
├── README.md                          ← 你正在阅读的文件
│
s_1.26 分支
├── IntlThrdPerfSchd.sln               # 恢复出的 VS 解决方案
├── IntlThrdPerfSchd/                  # 主源码工程 (net48)
├── _ilspy_export_nopdb/               # ILSpy 无 PDB 项目导出
├── _ilspy_export_cs8_nopdb/           # ILSpy 强制 C# 8 无 PDB 导出
├── _dnspy_export/                     # dnSpy 导出旁证
├── build-highfidelity.ps1             # 一键高保真审计脚本
├── shipped_payload/                   # 随包运行时文件
└── recovery_artifacts/                # 恢复证据、对比报告
│
n_2.35 / n_2.36 分支
├── IntlThrdPerfSchd.sln
├── IntlThrdPerfSchd/                  # 主源码工程 (net48)
├── _ilspy_export/ / _ilspy_export_cs8/
├── _dnspy_export/
├── shipped_payload/
├── recovery_artifacts/                # PDB 路径提取、DIA 方法映射等
├── PDB_RECOVERY_NOTES.md              # PDB 暴露的恢复记录
├── PROJECT_METADATA_RECOVERY.md       # 工程命名层恢复证据
└── ARCHITECTURE_ANALYSIS.md           # 架构与运行路径分析
│
n_2.51 分支
├── IntlThrdPerfSchd.sln
├── IntlThrdPerfSchd/
├── _ilspy_export/ / _ilspy_export_cs8/
├── _dnspy_export/
├── shipped_payload/                   # 含 permanent 权重备份
├── recovery_artifacts/
├── PDB_RECOVERY_NOTES.md
├── PROJECT_METADATA_RECOVERY.md
└── ARCHITECTURE_ANALYSIS.md
```

---

## 文件组织方式

每个版本分支内，恢复工程遵循一致的目录约定：

| 目录 / 文件 | 说明 |
| --- | --- |
| `IntlThrdPerfSchd.sln` | VS 解决方案入口（推断恢复） |
| `IntlThrdPerfSchd/` | 主源码工程，目标框架 `net48`；`AssemblyName = IntlThrdSchd`，`RootNamespace = IntlThrdPerfSchd` |
| `_ilspy_export*/` | ILSpy 反编译证据（C# 8 / 默认语言版本） |
| `_dnspy_export/` | dnSpy 反编译旁证 |
| `shipped_payload/` | 从原始发布包保留的运行时文件（WinRing0、TraceEvent、安装脚本等） |
| `recovery_artifacts/` | 机器生成的恢复证据，包括 IL /实体 / 元数据对比报告、PDB 路径提取、DIA compiland 映射等 |

---

## 快速开始

### 克隆特定版本

```bash
# 克隆整个仓库（所有分支）
git clone <repo-url>
cd <repo>

# 切换到目标版本分支
git checkout n_2.51    # 例如：2.51 神经网络版
```

### 普通构建（开发与调试）

```powershell
dotnet build .\IntlThrdPerfSchd.sln -c Release
```

### 高保真审计构建（仅 s_1.26）

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\build-highfidelity.ps1
```

该脚本执行 Release 构建后，使用 dnlib 将原始 EXE 中 8 个差异方法的 IL body 精确移植到 patched 产物，并进行全方法 IL hash、manifest resource、实体清单、元数据表面等多维对比验证。

---

## N 版关键模块

| 模块 | 文件 | 职责 |
| --- | --- | --- |
| 在线学习 | `OnlineLearning.cs` | 梯度计算与数学兼容层 |
| 调度模型 | `SchedulerModel.cs` | CircularBuffer、DecisionRecord、SchedulerStatistics、TransformerScheduler |
| 调度服务 | `SchedulerService.cs` | SchedulerService + SchedulerController |
| Transformer 层 | `TransformerLayers.cs` | Multi-Head Attention、FeedForward、LayerNorm、各类 Encoder |
| 线程追踪 | `ThreadPerformanceTracking.cs` | Tracker / Tracker4lat / ThreadPerformanceTracker |
| 实时调度 | `RealtimeScheduler.cs` | 实时调度逻辑与 RandomExtensions |
| 线程分类 | `ThreadClassifier.cs` | SOM 神经元聚类 |
| OpenLibSys | `OpenLibSys.cs` | WinRing0 硬件接口封装 |
| Windows 服务 | `Service1.cs` / `ProjectInstaller.cs` | 服务入口与安装器（含 Designer 文件） |

---

## N 版版本演进

| 对比维度 | 2.35 | 2.36 | 2.51 |
| --- | --- | --- | --- |
| PDB 匹配 | ✅ GUID/Age 匹配 | ✅ GUID/Age 匹配 | ❌ 无 PDB |
| PDB 源码根 | `IntlThrdPerfSchd3.29` | `IntlThrdPerfSchd4.6` | — |
| Transformer 编码器 | 基础 | + CoreTransformerEncoder<br>+ ThreadTransformerEncoder | 同 2.36 |
| 向量数学 | `MatrixOperations` / `VectorMath` | + `VectorMathNew` | 同 2.36 |
| 权重文件 | 无 | 无 | `scheduler_model.bin.bak`（永久权重） |
| 构建状态 | 179 W 0 E | 179 W 0 E | 0 W 0 E |
| 文本级 checksum 命中 | 2/19 文件 | 3/20 文件 | 不适用 |

---

## 恢复原则

1. **贴近原工程结构**：以 shipped PDB / DIA / named stream / InjectedSource 等二进制证据链驱动的文件边界恢复，不是简单堆叠反编译 dump。
2. **可编译可维护**：每棵树均可在 `dotnet build` 下零错误通过，是正常的二次开发起点。
3. **证据分离**：已证实的恢复与推断性恢复分开记录（`PDB_RECOVERY_NOTES.md` / `PROJECT_METADATA_RECOVERY.md`）。
4. **编译器噪音收敛**：显式设置 `LangVersion = 8.0`，将反编译器引入的现代语法还原为原作者工程语言级别。

---

## 注意事项

- 本仓库仅用于存档与分析目的（安全研究、兼容性分析、逆向工程技术参考）。
- 运行时依赖 WinRing0 驱动，且涉及 CPU MSR 操作——在物理机运行前请充分理解其行为。
- 构建和恢复笔记保存在各自版本分支中，请切换到对应分支查阅。
- 生成的二进制文件与恢复过程中的中间产物保持原始提交状态。
- 所有 PowerShell 脚本需要使用 **PowerShell 7 (`pwsh`)**，不要使用 Windows PowerShell 5.1。

---

## 相关资源

- [ILSpy](https://github.com/icsharpcode/ILSpy) — 反编译器
- [dnSpy](https://github.com/dnSpy/dnSpy) — .NET 调试与反编译工具
- [dnlib](https://github.com/0xd4d/dnlib) — .NET 程序集读写库
- [WinRing0](https://openlibsys.org/) — 硬件访问库
