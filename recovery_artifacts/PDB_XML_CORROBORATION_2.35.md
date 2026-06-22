# 2.35 PDB XML 旁证

## 背景
- 使用 Ghidra 自带 `pdb.exe` 对 shipped classic PDB 做了原始 XML 导出
- 命令形式：
  - `pdb.exe <input pdb file>`
- 原始 XML 落在临时目录，没有直接纳入仓库
  - 目的只是为现有 DIA / 字符串提取结论提供独立旁证

## 当前能确认的点
- XML 中未命中任何：
  - `.sln`
  - `.csproj`
- 这与当前：
  - PDB 字符串扫描
  - DIA compiland/source-file 映射
  - project metadata 恢复结论
  保持一致

## CoreIndexMapper 旁证
- XML 中直接存在：
  - `<symbol name="CoreIndexMapper" ... tag="Compiland" ... />`
- 同时 XML 中也存在多条：
  - `source_file="...\\IntlThrdPerfSchd\\CoreIndexMapper.cs"`
- 这进一步说明：
  - `CoreIndexMapper` 是 classic PDB 里的真实 compiland
  - 它之前缺席全量 compiland 图，确实只是筛选条件问题
  - 它不是“只有路径、没有可见 line-span”的假阳性文件

## 对当前恢复树的影响
- 当前继续保持：
  - `CoreIndexMapper.cs` 位于项目根
  - `CoreIndexMapper` 作为无命名空间顶层类型存在
- 当前继续保持：
  - `IntlThrdPerfSchd.sln`
  - `IntlThrdPerfSchd.csproj`
  的命名推断不变
- 因为 XML 旁证并没有提供任何新的 `.sln` / `.csproj` 直接线索
