# 2.35 PDB 源码根层级分析

## 结论
- 当前 2.35 classic PDB 能稳定证明一个事实：
  - 所有带可见 line-span 的源码文档都落在同一个项目根
  - `C:\Users\maxpp\source\repos\IntlThrdPerfSchd3.29\IntlThrdPerfSchd2.22\IntlThrdPerfSchd`
- 但它仍然不能直接证明：
  - 原始 `.sln` 文件名
  - 原始 `.sln` 所在目录名是否就是 `IntlThrdPerfSchd2.22`
  - 原始 repo 根是否需要直接映射为 `IntlThrdPerfSchd3.29`

## 量化结果
- `DIA_FULL_COMPILAND_MAP_2.35.json`
  - `compiland_count = 104`
  - `method_entry_count = 1732`
- 带可见 line-span 的唯一源码文件数：
  - `18`
- 唯一项目根数：
  - `1`

## 唯一项目根
- `C:\Users\maxpp\source\repos\IntlThrdPerfSchd3.29\IntlThrdPerfSchd2.22\IntlThrdPerfSchd`

## 项目根下已证实的源码文档
- `CoreIndexMapper.cs`
- `MatrixOperations.cs`
- `OnlineLearning.cs`
- `OnlineLearningManager.cs`
- `OpenLibSys.cs`
- `Program.cs`
- `ProjectInstaller.cs`
- `ProjectInstaller.Designer.cs`
- `RealtimeScheduler.cs`
- `SchedulerModel.cs`
- `SchedulerService.cs`
- `Service1.cs`
- `Service1.Designer.cs`
- `ThreadClassifier.cs`
- `ThreadDataPointV2.cs`
- `ThreadPerformanceTracking.cs`
- `TransformerLayers.cs`
- `VectorMath.cs`

## 额外文档
- `pdb_paths_2.35.txt` 还额外暴露了：
  - `obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs`
- 本轮修正后可以确认：
  - `CoreIndexMapper` 是一个**无命名空间**顶层 compiland
  - 它之前没有进入全量 compiland 图，只是因为当时的筛选条件只包含 `IntlThrdPerfSchd* / OpenLibSys / SimdLibrary`
  - 重新按 exact compiland `CoreIndexMapper` 抽取后，它已经进入当前带可见 line-span 的 compiland 图
- 当前仍然属于额外文档且不进入可见 line-span 图的主要只有：
  - `obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs`

## CompilandEnv 探测
- 对 classic PDB 的 `SymTagCompilandEnv` 做了直接探测
- 结果：
  - `env_compiland_count = 0`
- 因此当前没有从 PDB 获得可直接用于判断 solution 根的：
  - `cwd`
  - `cmd`
  - `src`
  - 其他编译环境字符串

## 当前对 solution 文件名的影响
- 当前证据足以支持：
  - 项目文件名继续采用 `IntlThrdPerfSchd.csproj`
- 当前证据也足以支持：
  - `CoreIndexMapper.cs` 保持为项目根下的无命名空间源码文件，而不是强行塞入 `IntlThrdPerfSchd` 命名空间
- 当前证据不足以支持：
  - 把 solution 文件名强行改成 `IntlThrdPerfSchd2.22.sln`
  - 或进一步改成 `IntlThrdPerfSchd3.29.sln`
- 因此当前保留：
  - `IntlThrdPerfSchd.sln`
  - 作为最保守、最少引入额外猜测的推断恢复名
