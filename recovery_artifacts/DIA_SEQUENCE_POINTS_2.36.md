# 2.36 Raw DIA Sequence-Point 证据

## 目标
- 为 2.36 当前最可能继续压保真度的 3 个模板化小文件补一层比 `method span` 更细的证据：
  - `ProjectInstaller.cs`
  - `ProjectInstaller.Designer.cs`
  - `Service1.Designer.cs`
- 把“已经被 raw line / column records 直接证明的事实”和“仍然只是强推断的文本壳层形态”分开。

## 工件
- 新脚本：
  - `recovery_artifacts/scripts/extract-dia-sequence-points.py`
- 新 JSON：
  - `recovery_artifacts/dia_sequence_points_2.36.json`

## 直接证据
- 当前目标过滤下共有 `29` 条 visible sequence-point records。
- `ProjectInstaller.cs`
  - visible lines:
    - `14`
    - `16`
    - `17`
    - `22`
    - `27`
  - method count:
    - `.ctor`
    - `serviceInstaller1_AfterInstall`
    - `serviceInstaller2_AfterInstall`
- `ProjectInstaller.Designer.cs`
  - visible lines:
    - `16`
    - `18`
    - `20`
    - `21`
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
  - method count:
    - `Dispose`
    - `InitializeComponent`
- `Service1.Designer.cs`
  - visible lines:
    - `16`
    - `18`
    - `20`
    - `21`
    - `31`
    - `32`
    - `33`
  - method count:
    - `Dispose`
    - `InitializeComponent`

## 高价值结论
- `Service1.Designer.cs` 与 `ProjectInstaller.Designer.cs` 的 `Dispose` 当前仍共享同一组 visible lines：
  - `16`
  - `18`
  - `20`
  - `21`
- 这强烈支持两点：
  - `Dispose` 前注释区更像 3 行 summary，而不是带 `<param>` 的更长 XML 块
  - 当前 remaining gap 主要仍在注释、字段声明和空白分布，而不是 `Dispose` 主体逻辑
- `ProjectInstaller.Designer.cs` 的 `InitializeComponent` 仍呈现明显分组结构：
  - `31-32`
  - `36-38`
  - `42-47`
  - `51-53`
  - `55`
- `ProjectInstaller.Designer.cs` 当前的 `51-53` 是一个跨 3 行的单 statement record。
  - 这直接排除了“单行 `AddRange(...)` 更接近原稿”的可能性。
- `Service1.Designer.cs` 的 `InitializeComponent` 仍然只有两条主体语句加一个 closing-brace visible point：
  - line `31`
  - line `32`
  - line `33`

## 列号带来的强推断
- `Service1.Designer.cs`
  - line `31` 的 `column_start = 13`、`column_end = 64`
    - 强烈支持它更像 `this.components = new System.ComponentModel.Container();`
  - line `32` 的 `column_start = 13`、`column_end = 43`
    - 可以排除无前缀的 `ServiceName = "Service1";`
    - 但仍不足以只靠列宽在 `this.ServiceName` 与 `base.ServiceName` 之间做最终裁决
- `ProjectInstaller.Designer.cs`
  - line `31` 的 `column_end = 97`
    - 强烈支持 `this.serviceProcessInstaller1 = new System.ServiceProcess.ServiceProcessInstaller();`
  - line `32` 的 `column_end = 83`
    - 强烈支持 `this.serviceInstaller1 = new System.ServiceProcess.ServiceInstaller();`
  - line `47` 的 `column_end = 142`
    - 强烈支持显式委托构造形式的事件绑定，而不是简写方法组
- `ProjectInstaller.cs`
  - `.ctor` 当前仍然只有 `14 / 16 / 17` 三个 visible points
  - 两个空 handler 则各自只剩一个 closing-brace visible point：
    - `22`
    - `27`
  - 这说明当前偏差仍然主要在非执行文本层，而不是 handler 主体逻辑

## 与 2.35 的关系
- 2.36 这一组小文件的 raw sequence-point 形态当前与 2.35 保持同型。
- 这说明：
  - 2.35 阶段已经总结出的 Designer / 模板化小文件收敛策略，当前仍然适用于 2.36
  - 但不能把 2.35 的最终文本壳层推断直接机械复制到 2.36，仍需保留“2.36 自身 sequence-point 只给出约束、未直接给出完整原稿文本”的边界

## 当前仍不能证明的内容
- 还不能仅凭 raw sequence points 钉死：
  - 注释到底是英文模板还是中文模板
  - 注释是 XML summary 风格还是旧式 `///`
  - `components` 字段的精确声明类型到底是 `IContainer` 还是 `Container`
  - `Service1.Designer.cs` 里到底是 `this.ServiceName` 还是 `base.ServiceName`
  - class 声明上是否带 `public`
- 因此这轮 sequence-point 工件的价值是：
  - 大幅缩小文本级搜索空间
  - 排除一批明显不对的模板化恢复写法
  - 但还不足以把这 3 个文件直接宣称为 checksum 已解出

## 对后续 2.36 深挖的影响
- `ProjectInstaller.Designer.cs`
  - 后续应优先围绕“全限定名对象创建 + 显式委托 + 多行 `AddRange` + 注释语言/风格”继续收敛
- `Service1.Designer.cs`
  - 后续应优先围绕“`this.ServiceName` / `base.ServiceName` 最终裁决 + 注释语言/风格”继续逼近
- `ProjectInstaller.cs`
  - 继续逼近时，应优先搜索注释、空白和 brace 布局，而不是方法体逻辑
