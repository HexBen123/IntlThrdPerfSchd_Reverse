# 2.36 Service1 生命周期尾部差异

## 目标
- 把 2.36 相对 2.35 的 `Service1` 剩余 6 处 method-map 差异继续压到 raw DIA line-record 级别。
- 分清三类情况：
  - 只是统一行号后移
  - 结构没变，但局部 line-span 发生了小范围偏移
  - 确实存在尾部源码表达式变化

## 输入工件
- 2.35 参考 sequence-point JSON：
  - `recovery_artifacts/service1_sequence_points_full_2.35_reference.json`
- 2.36 当前 sequence-point JSON：
  - `recovery_artifacts/service1_sequence_points_full_2.36.json`
- 自动比较脚本：
  - `recovery_artifacts/scripts/compare-service1-lifecycle-tail.py`
- 自动比较结果：
  - `recovery_artifacts/service1_lifecycle_tail_diff_2.35_vs_2.36.json`

## 当前恢复树中的定位
- `Service1.cs` 当前尾部关键位置：
  - `OnStart`：line `7260`
  - `thread()`：line `7554`
  - 2.36 新 guard：line `8081`
  - 2.36 新 `UpdateTAT(...)` 调用：line `8083`
  - `thread2()`：line `8097`
  - `OnStop`：line `8105`
  - `OnTimedEvent`：line `8110`
- 2.35 当前恢复树中的对应语句位置：
  - `if (sysinfo.accQcount > 0)`：line `8094`
  - `transformerScheduler.UpdateTAT(sysinfo.accRewordPerS / sysinfo.accQcount);`：line `8096`
  - `OnStop`：line `8118`
  - `OnTimedEvent`：line `8123`
- 这些是当前恢复树内的定位点，便于继续阅读源码；它们不是 shipped PDB 里的原始 line number。

## 自动比较结论
- `OnStart`
  - `262 -> 257`，是当前 6 个目标里唯一出现 visible line-record 数减少的方法。
  - 前 `16` 条 sequence points 完全一致，第一处 line drift 出现在原始 line `17321 -> 17323`。
  - 当前比较结果显示它不是“整段乱序”，而是尾部真实变化后引发的复杂 tail realignment。
  - 2.35 独有的最后 `5` 条 trailing rows 为：
    - `18165`
    - `18166`
    - `18185`
    - `18186`
    - `18198`
  - 2.36 末尾仍保留同型 closing/tail 结构，只是落在：
    - `18168`
    - `18169`
    - `18188`
    - `18189`
    - `18201`
- `<OnStart>g__thread1|704_0`
  - 分类：`StableStructureWithLocalizedOffset`
  - 前 `3` 条记录完全一致，后 `3` 条统一 `+2`。
  - 说明 `thread1` 本体没有出现新结构，只是被 `OnStart` 中部变化带动后移。
- `<OnStart>b__704_2`
  - 分类：`LocalizedSourceChangeWithStableStructure`
  - `365` 条记录里有 `358` 条完全不变。
  - line delta 只有最后 `7` 条出现 `+2`。
  - 第一处 column 变化出现在原始 line `17201`：
    - `column_end: 59 -> 85`
  - 第二处关键 column 变化出现在原始 line `17206`：
    - `column_end: 111 -> 118`
  - 这和 2.36 当前恢复树里更长的条件与调用表达式完全一致。
- `<OnStart>g__thread2|704_1`
  - 分类：`UniformLineOffsetOnly`
  - `4/4` 条记录全部统一 `+3`。
  - 说明 `thread2()` 本体没有新的结构或表达式变化。
- `OnStop`
  - 分类：`UniformLineOffsetOnly`
  - `2/2` 条记录全部统一 `+3`。
  - 说明 `OnStop()` 自身逻辑未变，只是被前面的尾部调整整体推后。
- `OnTimedEvent`
  - 分类：`StableStructureWithLocalizedOffset`
  - `31/31` 条记录总数不变。
  - line delta 以 `+3` 为主：
    - `3:3` 共 `28` 条
    - 剩余两类仅是 range closing 口径差异：
      - `3:1`
      - `1:1`
  - 当前没有 column 变化，说明 `OnTimedEvent()` 主体表达式本身并未出现新的文本扩展。

## 已确认的真实源码变化
- 当前 2.36 恢复树中，`OnStart` 尾部至少已经确认了两处真实表达式级变化：
- `if (sysinfo.accQcount > 0)` 当前最强文本候选变为 `if (sysinfo.accQcount > 0&&sysinfo.total_energy > 0)`
- `transformerScheduler.UpdateTAT(sysinfo.accRewordPerS / sysinfo.accQcount);` 当前最强候选变为 `transformerScheduler.UpdateTAT((float)sysinfo.accRewordPerS / sysinfo.accQcount);`
- 其中：
  - 原始 line `17206` 的 visible width 从 `74` 扩到 `81`
  - 双侧显式转换版本宽度会到 `88`，与 PDB 目标不符
  - 单侧显式转换版本宽度正好是 `81`，当前与 PDB 列宽完全对齐
- guard 行当前已进一步收敛：
  - 原始 line `17201` 的 visible width 从 `26` 扩到 `52`
  - 常规空格版本 `if (sysinfo.accQcount > 0 && sysinfo.total_energy > 0)` 的去缩进宽度是 `54`
  - `if (sysinfo.accQcount > 0&&sysinfo.total_energy > 0)` 的去缩进宽度正好是 `52`
  - 在当前候选枚举下，它是唯一自然且精确命中 target width 的文本候选
- 这两处变化与 `<OnStart>b__704_2` 在原始 line `17201` / `17206` 的 column 扩展直接对应。

## 当前最可信判断
- 2.36 的 `Service1` 生命周期尾部没有发生“整段重写”。
- 当前 6 处差异可以拆成：
  - 1 处真实尾部逻辑增强：
    - `OnStart` 中 energy/TAT 更新条件与表达式变长
  - 2 处局部函数/方法的结构性后移：
    - `<OnStart>g__thread1|704_0`
    - `OnTimedEvent`
  - 2 处纯统一偏移：
    - `<OnStart>g__thread2|704_1`
    - `OnStop`
  - 1 处直接被 column 变化证实的表达式级变化：
    - `<OnStart>b__704_2`

## 对后续 2.36 深挖的意义
- 当前不需要做整文件级 `Service1` 重排。
- 继续逼近 2.36 时，应把尾部精力集中到：
  - `OnStart` 中 energy/TAT 更新块附近的文本级恢复
  - `thread1` / lambda 闭包边界与 closing-brace 归属
  - 是否还存在与该块相邻的 1 到 2 处轻微表达式差异
- `OnStop` 和 `thread2` 当前已经没有继续深挖的高收益空间。
