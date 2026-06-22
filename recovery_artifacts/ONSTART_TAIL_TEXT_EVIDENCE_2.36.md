# 2.36 OnStart 尾部文本证据

## 目标
- 在不做整段重排的前提下，把 `Service1.cs` 中 `OnStart` 尾部 energy/TAT 更新块继续压到更接近 shipped 2.36 原稿的文本形态。

## 当前恢复树定位
- 文件：
  - `IntlThrdPerfSchd/Service1.cs`
- 当前关键行：
  - guard：line `8081`
  - `UpdateTAT(...)`：line `8083`

## 已知基线
- 2.35 当前恢复树对应语句：
  - `if (sysinfo.accQcount > 0)`
  - `transformerScheduler.UpdateTAT(sysinfo.accRewordPerS / sysinfo.accQcount);`
- 2.36 shipped PDB 的 raw sequence-point 关键变化：
  - 原始 line `17201`
    - `column_start = 33`
    - `column_end = 59 -> 85`
  - 原始 line `17206`
    - `column_start = 37`
    - `column_end = 111 -> 118`

## 宽度换算
- 宽度按 `column_end - column_start` 计算。
- 因而：
  - guard 行宽度：
    - 2.35 = `26`
    - 2.36 target = `52`
  - `UpdateTAT(...)` 行宽度：
    - 2.35 = `74`
    - 2.36 target = `81`

## 当前候选对比
- 2.35 基线：
  - `transformerScheduler.UpdateTAT(sysinfo.accRewordPerS / sysinfo.accQcount);`
  - 去缩进宽度 = `74`
- 双侧显式转换候选：
  - `transformerScheduler.UpdateTAT((float)sysinfo.accRewordPerS / (float)sysinfo.accQcount);`
  - 去缩进宽度 = `88`
- 单侧显式转换候选：
  - `transformerScheduler.UpdateTAT((float)sysinfo.accRewordPerS / sysinfo.accQcount);`
  - 去缩进宽度 = `81`
- guard 行候选：
  - 常规空格版本：
    - `if (sysinfo.accQcount > 0 && sysinfo.total_energy > 0)`
    - 去缩进宽度 = `54`
  - 去掉 `&&` 两侧空格版本：
    - `if (sysinfo.accQcount > 0&&sysinfo.total_energy > 0)`
    - 去缩进宽度 = `52`
  - 其它自然变体：
    - `if(sysinfo.accQcount > 0 && sysinfo.total_energy > 0)` = `53`
    - `if (sysinfo.accQcount > 0 & sysinfo.total_energy > 0)` = `53`
    - `if (0 < sysinfo.accQcount && 0 < sysinfo.total_energy)` = `54`

## 结论
- `UpdateTAT(...)` 这一行当前最强文本候选是：
  - `transformerScheduler.UpdateTAT((float)sysinfo.accRewordPerS / sysinfo.accQcount);`
- 原因：
  - 它是当前唯一与 2.36 shipped PDB target width `81` 完全对齐的自然表达式候选。
  - 双侧显式转换版本宽度 `88`，已明显超出 target width，不应继续保留为主候选。

## guard 行结论
- 当前 guard 的语义增强已经确认。
- 结合 target width `52` 与候选枚举结果，当前唯一自然且精确命中的文本候选是：
  - `if (sysinfo.accQcount > 0&&sysinfo.total_energy > 0)`
- 因而这条线当前已从“壳层开放项”进一步收敛到“`&&` 两侧无空格”的具体文本形态。

## 当前处理策略
- 立即收敛 `UpdateTAT(...)` 到单侧显式 `float` 转换版本。
- 当前已同步把 guard 行收敛到 `&&` 两侧无空格版本，并保留这一步的宽度证据。
