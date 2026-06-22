# 1.26 Source Fidelity Audit

This note tracks the source-primary recovery pass for the 8 methods that were historically covered by IL body transplant in `patch_method_bodies_1.26.ps1`.

The target is not to force the C# compiler to reproduce the original IL at any cost. The target is to keep ordinary source builds useful for maintenance and second-pass development while preserving the original 1.26 EXE as an audit baseline.

## Evidence Sources

- Current source: `recovered_src_1.26/IntlThrdPerfSchd/Service1.cs`
- Current source: `recovered_src_1.26/IntlThrdPerfSchd/OpenLibSys.cs`
- ILSpy no-PDB export: `recovered_src_1.26/_ilspy_export_cs8_nopdb/`
- dnSpy export: `recovered_src_1.26/_dnspy_export/`
- PDB/DIA side evidence: `recovery_artifacts/dia_method_map_1.26.json`
- Original IL mismatch report: `recovery_artifacts/method_il_hash_diff_1.26.txt`
- Patch target list: `recovery_artifacts/patch_method_bodies_1.26.ps1`

## Method Decisions

| Method | Evidence | Source decision |
|---|---|---|
| `Service1.OnStart(string[])` | DIA records `<OnStart>g__thread1|0`, `<OnStart>b__2`, and `<OnStart>g__thread2|1`; dnSpy shows the same local-function/display-class shape; ILSpy left unused array allocations as discard expressions. | Keep the local functions `thread1` and `thread2`; remove unused array allocations that only preserve decompiler/IL shape and do not feed runtime behavior. |
| `Service1/<>c__DisplayClass484_0::<OnStart>b__2(CSwitchTraceData)` | This compiler-generated method comes from the anonymous `ThreadCSwitch` callback inside `thread1`. | Do not edit directly; keep the source callback as the maintainable surface. It remains an audit-only IL patch target because compiler-generated member IL is not a good source-completion standard. |
| `Service1.OnTimedEvent(object, ElapsedEventArgs)` | It appears in the stable IL mismatch list, but the current source does not show obvious `goto IL_` labels or discard-only statements. | Leave behavior and structure unchanged in this pass. Future edits require stronger source-level evidence than IL hash drift. |
| `Service1.Intval2Limit(...)` | Current source contained `goto IL_0141` and an `IL_0141` label from decompiler control-flow recovery. | Replace the label/goto with a source-level boolean that preserves the early scheduling path from little cores without exposing IL labels in maintained source. |
| `Service1.UpdateNode(...)` | Current source contained discard reads of `node2.Next`, `num`, and `node_cap`; dnSpy/ILSpy variants also show decompiler-only unused reads around the same chain update pattern; later recovered trees use the clearer `current` / `prev` traversal style. | Remove no-op reads and the unused counter, then use `current`, `previous`, and `newNode` names while keeping the move-to-front linked-list behavior intact. |
| `Service1.UpdateNode1(...)` | Same pattern as `UpdateNode`, with a larger `Node1` payload. | Remove no-op reads and the unused counter, then use `current`, `previous`, and `newNode` names while keeping update, move-to-front, and insert behavior intact. |
| `Service1.UpdateNodeP(...)` | Same pattern as `UpdateNode`, with process-level `NodeP` payload. | Remove no-op reads and the unused counter, then use `current`, `previous`, and `newNode` names while keeping update, move-to-front, and insert behavior intact. |
| `OpenLibSys.Ols::.ctor()` | dnSpy shows a local DLL-name variable; current source already uses that shape but hardcoded the same strings despite existing constants `dllNameX64` and `dllName`. | Keep the local variable and route it through existing constants, preserving behavior while making the wrapper source less dump-like. |

## Boundary

The high-fidelity audit script may still patch these methods when generating `recovery_artifacts/patched/IntlThrdSchd.exe`. That output is useful for original-IL comparison, but ordinary development should start from the source build under `IntlThrdPerfSchd/bin/<Configuration>/net48/`.

## Validation

Executed from repository root on 2026-06-06:

```powershell
dotnet build .\recovered_src_1.26\IntlThrdPerfSchd.sln -c Release
```

Result:

- `129 warnings`
- `0 errors`

The warnings are the existing recovered-source unused-field warnings.

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\recovered_src_1.26\build-highfidelity.ps1
```

Result:

- `High-fidelity audit build verified`
- 8 patch target bodies were applied to the audit EXE.
- Stable method IL report matched `167/167`.
- Compiler-generated-inclusive method IL report matched `171/171`.
- Manifest resource compare reported `equal: True`.
- Metadata surface diff reported no missing or extra surface entries.
- Runtime payload verification completed.
