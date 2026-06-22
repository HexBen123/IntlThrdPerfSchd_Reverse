# Intel 大小核神经网络调度器 N 版 2.35 工程元数据恢复说明

## 1. 结论
- 当前 2.35 高保真树的项目文件名恢复为 `IntlThrdPerfSchd.csproj`。
- 当前解决方案文件补回为 `IntlThrdPerfSchd.sln`。
- 当前程序集名和资源根命名空间继续保持 `IntlThrdSchd`。

这不是“把所有名字统一改成一个词”，而是按不同证据层分别恢复：
- 源码树 / 项目层更像 `IntlThrdPerfSchd`
- 程序集 / EXE / 资源层更像 `IntlThrdSchd`

## 2. 直接证据
- shipped PDB 暴露的源码根路径最后一级目录明确是：
  - `C:\Users\maxpp\source\repos\IntlThrdPerfSchd3.29\IntlThrdPerfSchd2.22\IntlThrdPerfSchd\`
- shipped EXE 内 manifest resource 名称明确是：
  - `IntlThrdSchd.ProjectInstaller.resources`
- shipped EXE / 配置 / 安装链路明确围绕：
  - `IntlThrdSchd.exe`

## 3. 弱证据与参考证据
- 2.34 的已完成 `pdb_aligned_src` 参考树使用的是：
  - `IntlThrdPerfSchd\IntlThrdPerfSchd.csproj`
- shipped `安装服务.bat` 又同时使用服务名：
  - `IntlThrdPerfSchd`

这说明作者在“项目 / 源码目录命名”和“程序集 / EXE 命名”之间，本来就可能同时保留了 `PerfSchd` 与 `Schd` 两套词形。

## 4. 明确缺失的直接证据
- 当前再次扫描 shipped `PDB/EXE`，仍未直接命中：
  - `.csproj`
  - `.sln`
- 进一步使用 Ghidra `pdb.exe` 导出的 XML 旁证，也仍未直接命中：
  - `.csproj`
  - `.sln`
- 当前进一步直接探测 classic PDB 的 `SymTagCompilandEnv` 结果为：
  - `env_compiland_count = 0`
- 也就是说，当前没有从 PDB 再额外拿到：
  - `cwd`
  - `cmd`
  - `src`
  - 其他可直接指向 solution 根或原始 project 文件路径的 compiland 环境记录
- 因此以下内容仍不能宣称为“已证实”：
  - 原始 project 文件名
  - 原始 solution 文件名
  - 原始 project GUID
  - 原始 solution 内部配置矩阵
- 这一点的收口说明已单独整理到：
  - `recovery_artifacts/VS_METADATA_LIMITS_2.35.md`

## 4.1 单一项目根证据
- `DIA_FULL_COMPILAND_MAP_2.35.json` 当前确认：
  - `104` 个 compiland
  - `1732` 条方法级 line-span 记录
  - `18` 个唯一且带可见 line-span 的源码文件
- 这 `18` 个源码文件全部位于同一个项目根：
  - `C:\Users\maxpp\source\repos\IntlThrdPerfSchd3.29\IntlThrdPerfSchd2.22\IntlThrdPerfSchd`
- 结合 `pdb_paths_2.35.txt`，还可额外确认：
  - `CoreIndexMapper.cs` 的路径不仅存在于 PDB 文档列表，而且已经由 exact compiland `CoreIndexMapper` 映射钉实
  - `.NETFramework,Version=v4.8.AssemblyAttributes.cs` 的生成文件路径也存在于 PDB 文档列表
- 其中 `CoreIndexMapper` 还暴露出一个额外事实：
  - 它是一个**无命名空间**顶层类型
  - shipped EXE 的类型清单直接显示为 `Class CoreIndexMapper`
  - Ghidra `pdb.exe` XML 旁证也直接包含 `<symbol name="CoreIndexMapper" ... tag="Compiland" ... />`
- 这说明当前 PDB 能支持的是“单一 project root + 单个 namespace-less 根文件并存”这一层结论，而不是“原始 solution 文件具体叫什么名字”这一层结论

## 5. 当前恢复策略
- 项目文件名采用 `IntlThrdPerfSchd.csproj`
  - 原因：它和 PDB 源码根目录最后一级一致，也和 2.34 的高保真参考树一致
- 程序集名继续保持 `IntlThrdSchd`
  - 原因：shipped 发布物的 EXE 文件名就是 `IntlThrdSchd.exe`
- `RootNamespace` 显式保持 `IntlThrdSchd`
  - 原因：否则 `ProjectInstaller.resx` 编译出的 manifest resource 名将无法继续匹配 shipped `IntlThrdSchd.ProjectInstaller.resources`
- `LangVersion` 当前显式保持 `8.0`
  - 原因：当前恢复树已先去掉反编译器引入的 `file-scoped namespace`
  - 之后 fresh 验证表明：
    - `7.3` 过低，会被当前源码中的 `using` 声明、switch expression 与 `Vector<float>` 相关语法击穿
    - `8.0` 可以通过 fresh build
  - 因而 `8.0` 比先前的 `12.0` 更接近当前能被证据支持的最小语言级别
- 解决方案文件采用 `IntlThrdPerfSchd.sln`
  - 原因：它是对“源码根目录名”的延伸恢复，而不是对 EXE 文件名的机械复制
  - 同时，当前没有足够强的额外证据支持把它进一步改成 `IntlThrdPerfSchd2.22.sln` 或 `IntlThrdPerfSchd3.29.sln`

## 5.1 编译器指纹补充证据
- 当前对 shipped PDB 的原始字符串扫描，还额外直接命中了：
  - `C# - 5.3.0-2.26153.122+4d3023de605a78ba3e59e50c657eed70f125c68a`
  - `obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs`
  - `/src/headerblock`
  - 多条 `/src/files/...`
- 这些证据说明：
  - 当前 shipped PDB 内确实保留了 compiler/source-index 层痕迹
  - 但它们仍然不足以把“原作者工程文件里是否显式写过 `LangVersion`”直接钉死
- 对应收口已单独整理在：
  - `recovery_artifacts/COMPILER_LANGUAGE_FOOTPRINT_2.35.md`

## 5.2 InjectedSource 与特殊 Compiland 补充证据
- 当前进一步通过 `DIA COM` 直接确认：
  - `InjectedSource` 表真实存在
  - `SourceFiles.count = 19`
  - `InjectedSource.count = 19`
  - 两者文件名集合完全一致
- 这说明当前 19 个源码/生成文件文档，不只是 line-span 里被动出现，而是还被 PDB 以 injected-source 的形式再次索引。
- 当前全量 `SymTagCompiland` 也已确认是：
  - `106`
- 其中只有 2 个特殊项不属于常规源码 compiland：
  - `* CompilerInfo *`
  - `<DanglingDocuments*223343bd-a859-41a3-90c0-9dfa101f1a95>`
- 这个结果进一步支持：
  - 当前 project root / source file 集合已经基本完整
  - 但特殊 compiland 里仍然没有直接吐出 `.sln` / project GUID 等 VS 元数据
- 对应补强记录已单独整理在：
  - `recovery_artifacts/DIA_INJECTED_SOURCE_EVIDENCE_2.35.md`

## 5.3 InjectedSource payload 与源码文本级校验边界
- 本轮进一步确认：`InjectedSource` 并不只是“有一条记录”，而是携带了可解构的 payload。
- 当前 payload 头稳定解析为：
  - `C#`
  - `Microsoft`
  - `Text`
  - `SHA256`
- 这说明当前 classic PDB 至少保留了每个文档的**文本级哈希锚点**。
- 但这条证据也同时定义了一个更严格的上限：
  - 当前恢复树虽然在工程结构、文件边界、项目命名层和主要源码语义上已经非常接近原作者工程
  - 但如果按“文件文本 SHA-256 是否与原始源码完全一致”衡量，当前只有 `1/19` 个文档命中
- 因而当前恢复树更准确的定位应是：
  - 高保真的工程/源码语义恢复树
  - 而不是原始源码文本逐字节复刻树

## 6. 置信度说明
- `IntlThrdPerfSchd.csproj`
  - 中高置信恢复
- `RootNamespace = IntlThrdSchd`
  - 高置信恢复
- `IntlThrdPerfSchd.sln`
  - 中等置信推断恢复
- solution 内的 project GUID
  - 低置信占位恢复，仅用于让当前工程树具备正常打开和构建入口
