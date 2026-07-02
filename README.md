<div align="center">

# Intel 大小核线程调度器

**IntlThrdPerfSchd** — 基于 Transformer 神经网络的 Intel 大小核在线调度系统

![.NET](https://img.shields.io/badge/.NET%20Framework-4.8-512bd4)
![Language](https://img.shields.io/badge/C%23-LangVersion%208.0-239120)
![Platform](https://img.shields.io/badge/Platform-Windows%20x64%20-blue)
![Status](https://img.shields.io/badge/Type-逆向恢复存档-informational)
![Target](https://img.shields.io/badge/Target-Intel%2012th%2B%20P/E--core-critical)

</div>

---

> [!IMPORTANT]
> 本仓库为**逆向恢复源代码存档**，仅用于安全研究、兼容性分析与逆向工程技术参考。
> 原始程序运行时依赖 WinRing0 驱动并直接操作 CPU MSR 寄存器，在物理机运行前请充分理解其行为。

---

## 📖 项目简介

本仓库归档了 **Intel 大小核线程调度器** 的逆向恢复源代码。原始程序是一个基于 .NET Framework 4.8 的 Windows 服务，利用 Transformer 神经网络模型在线学习线程行为，动态将线程分配到 Intel 第 12 代及更新处理器的 **P-core（性能核）** 或 **E-core（能效核）** 上，从而优化系统整体性能与能效。

每次发布包均为**单一 `IntlThrdSchd.exe`**，不附带源码。本仓库通过 [ILSpy](https://github.com/icsharpcode/ILSpy) / [dnSpy](https://github.com/dnSpy/dnSpy) 反编译，结合 shipped PDB / DIA / PDB named stream 等二进制证据链，将各个发布版本逐一恢复为**可编译、可维护、贴近原作者工程结构**的 Visual Studio 解决方案。

---

## 🚀 快速开始

### 克隆并切换版本

```bash
# 克隆整个仓库（包含所有版本分支）
git clone <repo-url>
cd <repo>

# 列出全部可用版本
git branch -a

# 切换到目标版本分支
git checkout n_2.51    # 例如：2.51 神经网络版
```

### 构建

```powershell
dotnet build .\IntlThrdPerfSchd.sln -c Release
```

---

## 🔧 恢复原则

| 原则 | 说明 |
| :-- | :-- |
| **贴近原工程结构** | 以 shipped PDB / DIA / named stream / InjectedSource 等二进制证据链驱动文件边界恢复，而非简单堆叠反编译 dump。 |
| **可编译可维护** | 每个版本分支均可在 `dotnet build` 下零错误通过，是正常的二次开发起点。 |
| **证据分离** | 已证实的恢复与推断性恢复分开记录（见 `PDB_RECOVERY_NOTES.md` / `PROJECT_METADATA_RECOVERY.md`）。 |
| **编译器噪音收敛** | 显式设置 `LangVersion = 8.0`，将反编译器引入的现代语法还原为原作者工程语言级别。 |

---

## ⚠️ 注意事项

- 本仓库仅用于**存档与分析目的**（安全研究、兼容性分析、逆向工程技术参考）。
- 运行时依赖 WinRing0 驱动，且涉及 CPU MSR 操作 —— **在物理机运行前请充分理解其行为**。
- 构建与恢复笔记保存在各自版本分支中，请切换到对应分支查阅。
- 生成的二进制文件与恢复过程中的中间产物保持原始提交状态。

---

## 🔗 相关资源

| 工具 | 用途 |
| :-- | :-- |
| [ILSpy](https://github.com/icsharpcode/ILSpy) | .NET 反编译器 |
| [dnSpy](https://github.com/dnSpy/dnSpy) | .NET 调试与反编译工具 |
| [dnlib](https://github.com/0xd4d/dnlib) | .NET 程序集读写库 |
| [WinRing0](https://openlibsys.org/) | 硬件访问库（CPU MSR / 寄存器） |
