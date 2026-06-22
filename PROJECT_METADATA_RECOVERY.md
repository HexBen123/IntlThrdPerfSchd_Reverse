# Intel 大小核神经网络调度器 N 版 2.36 工程元数据恢复说明

## 1. 结论
- 当前 2.36 高保真树的项目文件名恢复为 `IntlThrdPerfSchd.csproj`。
- 当前解决方案文件补回为 `IntlThrdPerfSchd.sln`。
- 当前程序集名和资源根命名空间继续保持 `IntlThrdSchd`。

这不是“把所有名字统一改成一个词”，而是按不同证据层分别恢复：
- 源码树 / 项目层更像 `IntlThrdPerfSchd`
- 程序集 / EXE / 资源层更像 `IntlThrdSchd`

## 2. 直接证据
- shipped PDB 暴露的源码根路径最后一级目录明确是：
  - `C:\Users\maxpp\source\repos\IntlThrdPerfSchd4.6\IntlThrdPerfSchd2.22\IntlThrdPerfSchd\`
- shipped EXE 内 manifest resource 名称明确是：
  - `IntlThrdSchd.ProjectInstaller.resources`
- 当前恢复出的 `ProjectInstaller.resx` 编译产物已重新与 shipped EXE 内同名 resource 做字节级比对：
  - `origLength = 180`
  - `newLength = 180`
  - `equal = True`
- shipped EXE / 配置 / 安装链路明确围绕：
  - `IntlThrdSchd.exe`

## 3. 弱证据与参考证据
- 2.35 的已完成高保真恢复树使用的是：
  - `IntlThrdPerfSchd\IntlThrdPerfSchd.csproj`
  - `IntlThrdPerfSchd.sln`
- shipped `安装服务.bat` 当前又继续使用服务名：
  - `IntlThrdPerfSchd`

这说明 2.36 和 2.35 一样，项目/源码目录命名与程序集/发布物命名继续并存两套词形。

## 4. 明确缺失的直接证据
- 当前再次扫描 shipped `PDB/EXE`，还没有直接命中：
  - `.csproj`
  - `.sln`
- 当前已经继续做过：
  - `InjectedSource`
  - raw `named streams`
- 结果仍然没有新增：
  - `.csproj`
  - `.sln`
  - 其他可直接指向原始 solution / project 文件的正文或命名记录
- 当前还没有对 2.36 继续做：
  - `SymTagCompilandEnv`
  - `Ghidra pdb.exe XML`
  这类更深层工程元数据抽取。
- 因此以下内容仍不能宣称为“已证实”：
  - 原始 project 文件名
  - 原始 solution 文件名
  - 原始 project GUID
  - 原始 solution 内部配置矩阵

## 5. 当前恢复策略
- 项目文件名采用 `IntlThrdPerfSchd.csproj`
  - 原因：它和 PDB 源码根目录最后一级一致，也和 2.35 的高保真树一致
- 程序集名继续保持 `IntlThrdSchd`
  - 原因：shipped 发布物的 EXE 文件名就是 `IntlThrdSchd.exe`
- `RootNamespace` 显式保持 `IntlThrdSchd`
  - 原因：否则 `ProjectInstaller.resx` 编译出的 manifest resource 名将无法继续匹配 shipped `IntlThrdSchd.ProjectInstaller.resources`
- `LangVersion` 当前显式保持 `8.0`
  - 原因：当前恢复树已先去掉反编译器引入的 `file-scoped namespace`
  - 之后 fresh 验证表明：
    - `7.3` 过低，会被 `Vector<T>` 的“非托管构造类型”和 `Service1.cs` 中的 `using` 声明击穿
    - `8.0` 可以通过当前 project 与 solution 的 fresh build
  - 因而 `8.0` 比先前的 `12.0` 更接近当前能被证据支持的最小语言级别
- 解决方案文件采用 `IntlThrdPerfSchd.sln`
  - 原因：它是对“源码根目录名”的延伸恢复，而不是对 EXE 文件名的机械复制
  - 同时，当前没有足够强的额外证据支持把它进一步改成 `IntlThrdPerfSchd2.22.sln` 或 `IntlThrdPerfSchd4.6.sln`

## 6. 当前置信度说明
- `IntlThrdPerfSchd.csproj`
  - 中高置信恢复
- `AssemblyName / RootNamespace = IntlThrdSchd`
  - 高置信恢复
- `IntlThrdPerfSchd.sln`
  - 中等置信推断恢复
- solution 内的 project GUID
  - 低置信占位恢复，仅用于提供可打开的 solution 入口
