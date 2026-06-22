# Intel 大小核神经网络调度器 N 版 2.51 工程元数据恢复说明

本文记录 2.51 恢复树在“解决方案/项目/命名空间/程序集名”等元数据层面的恢复依据，以及哪些是直接证据、哪些是参考推断。

## 1. 结论
- 当前 2.51 高保真树的项目文件名使用：`IntlThrdPerfSchd.csproj`
- 当前解决方案文件使用：`IntlThrdPerfSchd.sln`
- 当前程序集名与资源根命名空间保持：`IntlThrdSchd`

这不是简单把所有名字统一成一个词，而是按不同证据层分别恢复：
- 源码树 / 项目层更像 `IntlThrdPerfSchd`（历史版本一致性 + 二进制字符串旁证）
- 程序集 / EXE / 资源层更像 `IntlThrdSchd`（发布物与资源命名的直接证据）

## 2. 直接证据（2.51 可复核）
- shipped 发布物文件名：
  - `IntlThrdSchd.exe`
- shipped EXE 内 manifest resource 名称（字符串可见）：
  - `IntlThrdSchd.ProjectInstaller.resources`
- shipped 安装脚本层服务名：
  - `安装服务.bat` 中仍使用 `IntlThrdPerfSchd`
- 二进制字符串扫描证据已固化：
  - `recovery_artifacts/binary_string_scan_2.51.txt`

## 3. 参考证据（跨版本一致性）
- 2.35/2.36 的高保真恢复树均使用：
  - `IntlThrdPerfSchd/IntlThrdPerfSchd.csproj`
  - `IntlThrdPerfSchd.sln`
- 2.51 的二进制字符串中仍出现 `IntlThrdPerfSchd`，与历史命名层现象同型。

## 4. 明确缺失的直接证据（2.51 的关键限制）
- 2.51 发布包未提供 `IntlThrdSchd.pdb`。
- `IntlThrdSchd.exe` 内也未发现 `RSDS/NB10` CodeView 签名或 `.pdb` 路径字符串。
- 因此以下内容不能宣称“已证实”，只能作为推断占位：
  - 原始 `.sln` 文件名与 solution GUID
  - 原始 `.csproj` 文件名与 project GUID
  - 原始 solution 配置矩阵（Debug/Release|x86/x64 等）

## 5. 当前恢复策略（为何这样命名）
- `AssemblyName = IntlThrdSchd`
  - 原因：发布物就是 `IntlThrdSchd.exe`
- `RootNamespace = IntlThrdSchd`
  - 原因：需要与 `IntlThrdSchd.ProjectInstaller.resources` 的资源根命名保持一致
- solution / csproj 命名使用 `IntlThrdPerfSchd`
  - 原因：与历史版本的工程层命名更一致，且二进制字符串仍出现该词形
- `TargetFramework = net48`、`PlatformTarget = x64`、`LangVersion = 8.0`
  - 依据：ILSpy 导出的可编译项目口径 + 当前恢复树可构建验证（见 `README.md`）
- `Release` 构建关闭调试目录（`DebugType=none`、`DebugSymbols=false`）
  - 依据：2.51 shipped `IntlThrdSchd.exe` 内未发现 `RSDS/NB10` 等调试目录签名

## 6. 当前置信度说明
- `AssemblyName / RootNamespace = IntlThrdSchd`
  - 高置信（直接证据）
- `IntlThrdPerfSchd.csproj`、`IntlThrdPerfSchd.sln`
  - 中等置信（跨版本一致性 + 二进制字符串旁证 + 工程可构建）
- GUID/solution 配置矩阵
  - 低置信占位（仅用于提供可打开的 solution 入口）
