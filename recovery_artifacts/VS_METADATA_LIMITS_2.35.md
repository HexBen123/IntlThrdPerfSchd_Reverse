# 2.35 VS 元数据恢复上限说明

## 结论
- 到当前为止，2.35 发布包已经足以高置信恢复：
  - 源码 project root
  - 项目文件命名倾向
  - 程序集名与资源根命名空间
  - 单个无命名空间顶层文件 `CoreIndexMapper.cs`
- 但仍然**不足以直接恢复**：
  - 原始 solution 文件名
  - 原始 project GUID
  - 原始 solution GUID
  - 原始 solution 内部配置矩阵

## 为什么恢复不到

### 1. 字符串层没有命中
- shipped `PDB/EXE` 的字符串扫描到目前仍未直接命中：
  - `.sln`
  - `.csproj`

### 2. DIA CompilandEnv 为空
- classic PDB 的 `SymTagCompilandEnv` 探测结果为：
  - `env_compiland_count = 0`
- 因此当前缺失：
  - `cwd`
  - `cmd`
  - `src`
  - 其他能把 solution 根或 project 文件完整路径钉死的编译环境信息

### 3. PDB XML 旁证没有新增 VS 元数据
- Ghidra `pdb.exe` 导出的 XML 旁证同样未命中：
  - `.sln`
  - `.csproj`
- 它能旁证 compiland 和 source file，但不能补出 Visual Studio solution 元数据

### 4. 当前 `.sln` 是功能性恢复，不是证据性恢复
- 当前仓库内的 `IntlThrdPerfSchd.sln` 是为了：
  - 让工程树具备可打开、可构建、可回退的正常入口
- 它不是从 shipped 发布包里直接抠出来的
- 其中 project GUID / solution GUID 都只是占位型恢复

## 当前能恢复到哪一层
- 可以恢复到：
  - `IntlThrdPerfSchd.csproj` 这一项目名层
  - `IntlThrdSchd` 这一程序集/资源名层
  - `IntlThrdPerfSchd3.29\\IntlThrdPerfSchd2.22\\IntlThrdPerfSchd` 这一源码目录层
- 不能再无证据地跨过去恢复到：
  - 真正的 `.sln` 文件名
  - 真正的 project GUID / solution GUID

## 对后续工作的意义
- 以后如果没有新的 sidecar 证据，例如：
  - 原始 `.sln`
  - `.suo`
  - `.user`
  - MSBuild binlog
  - 更完整的 CI / 打包脚本
  - 额外 classic PDB 辅助流
- 那么继续在当前 shipped 包上追 `solution GUID / project GUID`，收益会很低
- 当前更合理的做法是：
  - 把这部分明确标记为“恢复上限”
  - 避免后续重复投入
