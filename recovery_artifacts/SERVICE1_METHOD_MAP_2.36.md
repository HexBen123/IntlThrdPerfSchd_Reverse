# 2.36 Service1 Method Map

This document is generated from the shipped classic PDB via DIA method-to-source line-span extraction.

## Inputs
- PDB:
  - `G:\BaiduNetdiskDownload\逆向\Intel大小核神经网络调度器N版2.36\IntlThrdSchd\IntlThrdSchd.pdb`
- Generated JSON:
  - `G:\BaiduNetdiskDownload\逆向\recovered_src_2.36\recovery_artifacts\SERVICE1_METHOD_MAP_2.36.json`

## Summary
- Compilands matched by prefix `IntlThrdPerfSchd.Service1*`: `63`
- Method entries with line spans: `1315`
- `Service1.cs` entries: `1313`
- `Service1.Designer.cs` entries: `2`

## Direct Conclusions
- 2.36 的 `Service1*` compiland 数和 method-entry 数与 2.35 当前完全一致：
  - `compiland_count = 63`
  - `method_entry_count = 1315`
- 这说明 2.36 的 `Service1` 总体拓扑没有发生“大块重构”级变化。

## Normalized Comparison Against 2.35
- 将 2.35 与 2.36 的 `Service1*` method map 做如下归一化后对比：
  - 统一 `method_name` 口径
  - 统一 `lexical_parent` 的 `/` 与 `.` 表达差异
  - 忽略源码根路径版本号差异
  - 忽略 token 编号漂移
  - 按 `source_file + line_start + compiland + method_name` 排序
- 当前结果：
  - 总体规模完全一致
  - 归一化后只剩 `6` 处差异

## 2.35 -> 2.36 的 6 处已知差异
- `OnStart`
  - `12932-18198` -> `12932-18201`
  - `statement_count: 262 -> 257`
  - `line_record_count: 262 -> 257`
- `<OnStart>g__thread1|704_0`
  - `12962-17309` -> `12962-17311`
- `<OnStart>b__704_2`
  - `12980-17282` -> `12980-17284`
- `<OnStart>g__thread2|704_1`
  - `18175-18183` -> `18178-18186`
- `OnStop`
  - `18206-18208` -> `18209-18211`
- `OnTimedEvent`
  - `18214-18715` -> `18217-18716`

## 当前意义
- 当前 `Service1` 的差异已经被收窄到服务生命周期尾部：
  - `OnStart`
  - 其局部函数
  - `OnStop`
  - `OnTimedEvent`
- 这说明：
  - 2.36 继续深挖 `Service1` 时，优先级已经不需要放在“全文件大范围重排”
  - 应改成围绕 `OnStart` 尾部和服务生命周期收口段做定点顺序/文本逼近
