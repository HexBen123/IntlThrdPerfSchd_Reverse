# 2.35 模板化小文件 Checksum 搜索边界

## 目标
- 继续逼近以下 3 个小文件的文本级 `SHA-256` 命中：
  - `Service1.Designer.cs`
  - `ProjectInstaller.Designer.cs`
  - `ProjectInstaller.cs`

## 当前目标 checksum
- `Service1.Designer.cs`
  - `55ebaa1a84f3b504cbb7d938d18cceca024495279e7904d47ee17c1a20e30b6b`
- `ProjectInstaller.Designer.cs`
  - `6013fffa85739d2796c81344ef609335bc21701eb69b3a85413c04e238e16140`
- `ProjectInstaller.cs`
  - `5eaf364e732ef11197449d06b1c09aa3cf61509b52cc2823b8c2880741455b9d`

## 2.34 可参考版本
- `2.34版逆向理论资料/pdb_aligned_src/IntlThrdPerfSchd/Service1.Designer.cs`
- `2.34版逆向理论资料/pdb_aligned_src/IntlThrdPerfSchd/ProjectInstaller.Designer.cs`
- `2.34版逆向理论资料/pdb_aligned_src/IntlThrdPerfSchd/ProjectInstaller.cs`
- 这些 2.34 参考文件当前都更像“极简 file-scoped namespace + 无 Designer 注释”的恢复形态，不足以直接命中 2.35 的 checksum。

## 关键约束修正
- 对 Designer 文件，PDB line span 更合理的解释不是“方法签名所在行”，而是“首个 sequence point / 首条可执行语句所在行”。
- 这会影响模板搜索时的行号约束：
  - `Service1.Designer.cs`
    - `Dispose`: `16-21`
    - `InitializeComponent`: `31-33`
  - `ProjectInstaller.Designer.cs`
    - `Dispose`: `16-21`
    - `InitializeComponent`: `31-55`
- 其中 `ProjectInstaller.Designer.cs` 当前恢复稿里第一条可执行语句已经落在 `31` 行，说明它比 `Service1.Designer.cs` 更接近原稿；剩余差异更像注释、region、限定名和语句间空行。

## 已执行搜索

### 1. `Service1.Designer.cs`
- 已搜索空间包括：
  - `block-scoped namespace`
  - `public partial` / `partial`
  - `IContainer` / `System.ComponentModel.IContainer`
  - `components = new Container()` / `new System.ComponentModel.Container()`
  - `base.ServiceName` / `this.ServiceName` / `ServiceName`
  - 经典 VS Designer `#region` 与 `summary` 注释的多个尾随空格变体
  - `UTF-8` / `UTF-8 BOM`
  - `CRLF` / `LF`
  - `4 空格` / `tab`
- 已跑过两轮较大的约束搜索：
  - 早期宽搜索：`24576` 个候选
  - 后续按 sequence-point 行号收窄后，仍未命中
- 结论：
  - 当前剩余差异很可能落在更细的生成器排版、空白行分布或未覆盖注释文本细节，而不是结构方向错误。

### 2. `ProjectInstaller.cs`
- 已搜索空间包括：
  - 有/无 `using System.ServiceProcess`
  - `public partial class` / `public class` / `partial class`
  - `InitializeComponent()` / `this.InitializeComponent()`
  - 空 handler 单行 `{ }`
  - 若干 XML summary 变体
  - `UTF-8` / `UTF-8 BOM`
  - `CRLF`
- 一个重要约束是：
  - shipped PDB 里两个 `AfterInstall` 方法都是单行 span
  - 说明原稿很可能不是三行空块，而是单行空 handler
- 当前已跑过一轮按行号约束的组合搜索：
  - `2592` 个候选
  - 未命中
- 结论：
  - 当前文件虽小，但剩余差异仍不足以仅靠现有 line span + 常规模板空间直接反推出精确原稿。

### 3. `ProjectInstaller.Designer.cs`
- 已搜索空间包括：
  - `IContainer` / `System.ComponentModel.IContainer`
  - `this.` 限定名
  - `InstallEventHandler(...)` 显式委托 / 直接方法组
  - `base.Installers.AddRange(new Installer[2] ...)`
  - `this.Installers.AddRange(new Installer[] ...)`
  - `#region` 与 `summary` 注释多种尾随空格变体
  - `UTF-8` / `UTF-8 BOM`
  - `CRLF`
  - `tab` / `4 空格`
- 按当前 line span 和典型 Designer 模板约束跑过一轮聚焦搜索：
  - `576` 个候选
  - 未命中
- 结论：
  - 当前文件比 `Service1.Designer.cs` 更接近原稿，但仍缺少足够强的文本级旁证去钉死最终排版。

## 当前判断
- 当前发布包内部关于工程资产和 source-index 的隐藏证据已经基本榨干：
  - `/src/files/...`
  - `/names`
  - `/src/headerblock`
  都已经被解析并证实不再提供额外源码正文或工程元数据。
- 因而这 3 个小文件剩余的 checksum 差异，当前更像：
  - 生成器细节
  - 注释/region 文本微差
  - 限定名/空白行/编码细差
- 在没有新的 sidecar 证据前，继续穷举仍可能命中，但收益已经明显进入低回报区。

## 本轮新增 raw sequence-point 证据
- 新工件：
  - `DIA_SEQUENCE_POINTS_2.35.md`
  - `dia_sequence_points_2.35.json`
- 当前已经能被 raw line records 直接证明的点：
  - `Service1.Designer.cs`
    - `Dispose` visible lines 固定为 `16/18/20/21`
    - `InitializeComponent` visible lines 固定为 `31/32/33`
  - `ProjectInstaller.Designer.cs`
    - `Dispose` visible lines 同样固定为 `16/18/20/21`
    - `InitializeComponent` 的 visible lines 固定为：
      - `31`
      - `32`
      - `36`
      - `37`
      - `38`
      - `42`
      - `43`
      - `44`
      - `45`
      - `46`
      - `47`
      - `51-53`
      - `55`
  - `ProjectInstaller.cs`
    - `.ctor` visible points 为 `14/16/17`
    - 两个空 handler 的唯一 visible point 分别是 `22` 和 `27`
- 这轮最重要的约束收紧：
  - `Dispose` 前的注释区更像 3 行 summary，而不是带 `<param>` 的更长 XML 块
  - `ProjectInstaller.Designer.cs` 的 `AddRange(...)` 原稿明显更像跨 `51-53` 的多行 statement，而不是当前恢复稿的一行写法
  - `ProjectInstaller.Designer.cs` line `31/32/47` 的列宽强烈支持：
    - 全限定名对象创建
    - 显式委托构造形式的事件绑定
  - `Service1.Designer.cs` line `31` 的列宽强烈支持：
    - `this.components = new System.ComponentModel.Container();`
- 这轮仍然没有被直接钉死的内容：
  - 注释语言是英文模板还是中文模板
  - 注释是 XML summary 还是旧式 `///`
  - `components` 字段的精确声明类型
  - `Service1.Designer.cs` 的 `this.ServiceName` vs `base.ServiceName`

## 建议的下一步优先级
1. 继续优先打 `ProjectInstaller.cs`
   - 文件最小，仍然最有机会先命中
2. 然后是 `ProjectInstaller.Designer.cs`
   - 结构已经很接近，剩余更像排版微差
3. 最后再继续 `Service1.Designer.cs`
   - 搜索空间最大，且当前缺少更强旁证
