# 2.81 Source Fidelity Audit

This note tracks the current source-primary recovery pass for `Intel大小核神经网络调度器N版2.81永久权重全功能版`.

The final target is `100%` recovery of the original author's true project structure and source code. The target is not to force unnatural C# into the source tree just to reproduce compiler IL; the source tree must stay readable and buildable, and every fidelity claim must be backed by local binary evidence or stronger source evidence.

## Current Fidelity Position

- Source recovery progress estimate: `97% / 100%`
- Ordinary source build: passed
- Build result: `180 warnings`, `0 errors`
- Stable method IL match: `1902 / 1923 = 98.908%`
- Entity count: `194 / 194`
- Manifest resource bytes: equal
- Metadata surface count: `11909 / 11909`

## Evidence Sources

- Original EXE: `recovery_artifacts/original/IntlThrdSchd.exe`
- ILSpy default export: `_ilspy_export/`
- ILSpy C# 8 export: `_ilspy_export_cs8/`
- dnSpy export: `_dnspy_export/`
- Main recovered source: `IntlThrdPerfSchd/`
- Closest historical N-series source tree: `../recovered_src_2.51/`

## Reports

- `manifest_resource_compare_2.81.txt`
  - `IntlThrdSchd.ProjectInstaller.resources` is byte-equal.
- `entities_diff_2.81.txt`
  - Entity counts match.
  - Remaining difference is a compiler-generated `OnlineLearningManager` display class number drift.
- `method_il_hash_diff_2.81.txt`
  - Stable method keys all exist in both original and recovered output.
  - 21 method bodies still hash differently.
- `metadata_surface_diff_2.81.txt`
  - Metadata surface counts match.
  - Remaining missing/extra lines mainly represent compiler-generated member numbering and cached lambda field numbering drift.

## Major Source Decisions

| Area | Evidence | Current decision |
|---|---|---|
| Project root namespace | Original manifest resource is `IntlThrdSchd.ProjectInstaller.resources`; recovered build initially emitted `IntlThrdPerfSchd.ProjectInstaller.resources`. | Set `RootNamespace` to `IntlThrdSchd` while keeping source namespace `IntlThrdPerfSchd`, matching the 2.51 recovery pattern and original resource surface. |
| Main source baseline | 2.81 has no observed matching PDB; ILSpy and dnSpy both decompile the assembly successfully. | Use ILSpy C# 8 export as the main source baseline; retain dnSpy export as cross-check evidence. |
| Payload handling | 2.81 ships runtime DLLs, WinRing0 files, TraceEvent/DIA dependencies, service scripts, and `powerreg.reg`. | Store runtime/install files in `shipped_payload/` and copy them through the project file. |
| `ProjectInstaller` file boundary | ILSpy and dnSpy emit a single decompiled file, while prior recovered service projects use a Visual Studio designer split. The split does not change service installer behavior. | Split into `ProjectInstaller.cs` and `ProjectInstaller.Designer.cs`; keep constructor and event callbacks in the main file, and fields, `Dispose`, and `InitializeComponent` in the designer file. |
| Method mismatch handling | 21 stable method IL hashes differ even though entity counts match. | Do not add no-op source or `goto IL_####` solely for IL shape. Track the mismatches for future source-level audit; patched EXE body transplant, if added later, must remain audit-only. |
| 2026-07-01 source-shape pass | Method-level diffs showed natural source structure gaps: lock return placement, auto-property initializer order, early returns, explicit span locals, unary negation, and explicit layer training arguments. | Updated `RealtimeScheduler`, `OnlineLearningManager`, `Service1.ProcessBehaviorAnalyzer`, `LinearLayer`, `LayerNormLayer`, `ThreadFFNBlock`, `CoreTransformerEncoder`, `MultiHeadAttention`, `TransformerScheduler`, and selected `CrossAttentionScheduler` methods only where the source shape is defensible. |
| 2026-07-02 remaining-mismatch pass | dnSpy and IL diffs showed natural source structure gaps: unary negative expression shape, explicit `if/else` learning-rate branches, reused linked-list cursor locals, `Intval2Limit` branch structure, fixed-window normalization control flow, null-conditional array length access, and explicit `TransformerScheduler.Schedule` learning-rate branches. | Updated `Service1.CrossAttentionScheduler.DistributeReward`, `Service1.NeuralNetwork.UpdateWeights`, `Service1.UpdateGroupInfo`, `Service1.UpdateThreadInfoSimp*`, `Service1.Intval2Limit`, `TransformerScheduler.UpdateNormalizationFixedWindow`, `TransformerScheduler.Schedule`, and selected dnSpy-backed `TransformerScheduler` source expressions. Skipped no-op-only `pop` gaps and compiler local-slot drift. |
| 2026-07-02 SIMD source-shape pass | IL local signatures and dnSpy exports showed natural source structure gaps in `VectorMathNew`: combined `fixed` statements for `ComputeMinMaxPerColumn`, and explicit `vector6` normalization reuse in `LayerNormForward`. | Updated `VectorMathNew.ComputeMinMaxPerColumn` and `VectorMathNew.LayerNormForward`. Stable method IL match improved from `1900 / 1923 = 98.804%` to `1902 / 1923 = 98.908%`. Skipped remaining no-op-only `pop`, dummy-local, branch-shortening, and compiler-generated numbering drift. |
| 2026-07-02 residual classification pass | Direct normalized IL diffs were run for `Service1.OnStart`, `Service1.UpdateNode1`, `Service1/CrossAttentionScheduler.Schedule`, `TransformerScheduler.GetAttentionHeadReport`, `TransformerScheduler.PerformBatchTraining`, `TransformerScheduler.UpdateTATInternal`, `OnlineLearningManager.UpdateModel`, and closure cleanup methods. The remaining differences are local-slot/signature drift, old compiler `callvirt` versus current compiler `call`, tuple-field lowering (`dup`/`pop` versus direct `.Item1`), `beq` versus `ceq`/`brtrue`, generated closure cache strategy, display-class numbering, and no-op-only `pop` sequences. | No source change was retained from this pass. Natural tuple deconstruction trials for `CrossAttentionScheduler.Schedule` built successfully but were optimized back to direct `.Item1`, so they did not improve the method hash. The source tree remains at `1902 / 1923 = 98.908%`; the residual mismatch list is now classified rather than unexplored. |

## Remaining Mismatch Categories

The current `method_il_hash_diff_2.81.txt` mismatch list is concentrated in these classified areas:

- Service lifecycle and callback reconstruction:
  - `Service1.OnStart` has the same normalized instruction count as the original. The first instruction-level drift is `ManagementObjectSearcher.Get()` emitted as old-compiler `callvirt` in the original and `call` in the current build; metadata drift also includes generated local-function callback numbering.
- Scheduling and linked-list update logic:
  - `Service1.UpdateNode`, `Service1.UpdateNode1`, and `Service1.UpdateNodeP` differ at discard-only argument/local reads before return.
  - `Service1.ProcessCompare*` differs through unused locals and discard-only stack cleanup.
- Online learning and tracker statistics:
  - `OnlineLearningManager.GetStats`, `OnlineLearningManager.UpdateModel`, `Tracker.RecalculateMaxValue`, and `Tracker4lat.RecalculateMaxValue` are dominated by compiler-generated `<>c` / display-class numbering drift.
- Transformer scheduling:
  - `TransformerScheduler.GetAttentionHeadReport` differs by `beq` versus `ceq`/`brtrue` branch lowering.
  - `TransformerScheduler.PerformBatchTraining` differs by local-slot ordering around repeated gradient arrays.
  - `TransformerScheduler.UpdateTATInternal` differs at a no-op-only branch body containing `ldarg.1`, `ldc.r4 2`, and paired `pop`.
- Cross-attention and generated closure drift:
  - `Service1/CrossAttentionScheduler.Schedule` differs by ValueTuple field access lowering (`dup` + `ldfld Item1` + `pop` in the original, direct `.Item1` in the current compiler output).
  - `Service1/CrossAttentionScheduler.TrainOnline` differs by discard-only argument cleanup.
  - `Service1/SchedulerDataset.ComputeNorm`, `Service1/ThreadClassifier.CleanExpiredData`, and `Service1/ProcessBehaviorAnalyzer.CleanInactiveThreads` differ by lambda/display-class delegate caching strategy.
## Boundary

The current source tree is buildable and structurally aligned at the entity/resource level, but it is not yet a `100%` original-author-source recovery. The strongest current statement is:

- ordinary source build is usable;
- resource bytes are identical;
- stable entity keys are present;
- stable method IL hash match is `98.908%`;
- remaining gaps are known and captured for the next fidelity pass.
