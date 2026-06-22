#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from dataclasses import dataclass
from datetime import datetime, timezone
from difflib import SequenceMatcher
from pathlib import Path
from typing import Any


TARGET_METHODS = [
    "OnStart",
    "<OnStart>g__thread1|704_0",
    "<OnStart>b__704_2",
    "<OnStart>g__thread2|704_1",
    "OnStop",
    "OnTimedEvent",
]

TARGET_SOURCE_MARKERS = {
    "OnStart": "protected override void OnStart(string[] args)",
    "thread1_local": "void thread()",
    "energy_guard": "if (sysinfo.accQcount > 0&&sysinfo.total_energy > 0)",
    "update_tat_call": "transformerScheduler.UpdateTAT((float)sysinfo.accRewordPerS / sysinfo.accQcount);",
    "thread2_local": "void thread2()",
    "OnStop": "protected override void OnStop()",
    "OnTimedEvent": "private void OnTimedEvent(object sender, ElapsedEventArgs e)",
}

BASE_SOURCE_MARKERS = {
    "energy_guard": "if (sysinfo.accQcount > 0)",
    "update_tat_call": "transformerScheduler.UpdateTAT(sysinfo.accRewordPerS / sysinfo.accQcount);",
    "OnStop": "protected override void OnStop()",
    "OnTimedEvent": "private void OnTimedEvent(object sender, ElapsedEventArgs e)",
}


@dataclass(frozen=True)
class SequenceKey:
    line: int
    line_end: int
    column_start: int
    column_end: int


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def find_line_numbers(source_path: Path, markers: dict[str, str]) -> dict[str, int | None]:
    lines = source_path.read_text(encoding="utf-8").splitlines()
    result: dict[str, int | None] = {}
    for label, needle in markers.items():
        match = None
        for index, line in enumerate(lines, start=1):
            if needle in line:
                match = index
                break
        result[label] = match
    return result


def rows_for_method(payload: dict[str, Any], method_name: str) -> list[dict[str, Any]]:
    rows = [
        row
        for row in payload["sequence_points"]
        if row["source_file_base_name"] == "Service1.cs" and row["method_name"] == method_name
    ]
    rows.sort(
        key=lambda item: (
            item["line"],
            item["line_end"],
            item["column_start"],
            item["relative_virtual_address"],
        )
    )
    return rows


def key_for_row(row: dict[str, Any]) -> SequenceKey:
    return SequenceKey(
        line=int(row["line"]),
        line_end=int(row["line_end"]),
        column_start=int(row["column_start"]),
        column_end=int(row["column_end"]),
    )


def summarize_deltas(base_rows: list[dict[str, Any]], target_rows: list[dict[str, Any]]) -> dict[str, Any]:
    paired_count = min(len(base_rows), len(target_rows))
    line_delta_pairs: dict[str, int] = {}
    first_line_change = None
    first_column_change = None
    line_shift_only_count = 0
    exact_pair_count = 0
    column_changed_pair_count = 0

    for index in range(paired_count):
        base_row = base_rows[index]
        target_row = target_rows[index]
        delta = (
            int(target_row["line"]) - int(base_row["line"]),
            int(target_row["line_end"]) - int(base_row["line_end"]),
        )
        delta_key = f"{delta[0]}:{delta[1]}"
        line_delta_pairs[delta_key] = line_delta_pairs.get(delta_key, 0) + 1

        same_columns = (
            int(base_row["column_start"]) == int(target_row["column_start"])
            and int(base_row["column_end"]) == int(target_row["column_end"])
        )
        same_lines = delta == (0, 0)

        if same_lines and same_columns:
            exact_pair_count += 1
        elif not same_lines and same_columns:
            line_shift_only_count += 1
        elif not same_columns:
            column_changed_pair_count += 1

        if first_line_change is None and not same_lines:
            first_line_change = {
                "pair_index": index,
                "base": key_for_row(base_row).__dict__,
                "target": key_for_row(target_row).__dict__,
            }
        if first_column_change is None and not same_columns:
            first_column_change = {
                "pair_index": index,
                "base": key_for_row(base_row).__dict__,
                "target": key_for_row(target_row).__dict__,
            }

    return {
        "paired_count": paired_count,
        "exact_pair_count": exact_pair_count,
        "line_shift_only_pair_count": line_shift_only_count,
        "column_changed_pair_count": column_changed_pair_count,
        "line_delta_histogram": line_delta_pairs,
        "first_line_change": first_line_change,
        "first_column_change": first_column_change,
    }


def longest_matching_blocks(base_rows: list[dict[str, Any]], target_rows: list[dict[str, Any]]) -> dict[str, Any]:
    base_keys = [key_for_row(row) for row in base_rows]
    target_keys = [key_for_row(row) for row in target_rows]
    matcher = SequenceMatcher(a=base_keys, b=target_keys, autojunk=False)
    blocks = [
        {"base_index": block.a, "target_index": block.b, "size": block.size}
        for block in matcher.get_matching_blocks()
        if block.size > 0
    ]
    matched_item_count = sum(block["size"] for block in blocks)
    longest_block_size = max((block["size"] for block in blocks), default=0)
    opcodes = [
        {
            "tag": tag,
            "base_start": i1,
            "base_end": i2,
            "target_start": j1,
            "target_end": j2,
        }
        for tag, i1, i2, j1, j2 in matcher.get_opcodes()
        if tag != "equal"
    ]
    return {
        "matched_item_count": matched_item_count,
        "matched_ratio_against_base": (matched_item_count / len(base_rows)) if base_rows else 0.0,
        "matched_ratio_against_target": (matched_item_count / len(target_rows)) if target_rows else 0.0,
        "longest_equal_block_size": longest_block_size,
        "equal_blocks": blocks,
        "non_equal_opcodes": opcodes,
    }


def classify_method(
    base_rows: list[dict[str, Any]],
    target_rows: list[dict[str, Any]],
    delta_summary: dict[str, Any],
    matching_summary: dict[str, Any],
) -> str:
    delta_keys = set(delta_summary["line_delta_histogram"].keys())
    counts_equal = len(base_rows) == len(target_rows)
    no_column_changes = delta_summary["column_changed_pair_count"] == 0
    first_line_change = delta_summary["first_line_change"]
    if (
        counts_equal
        and no_column_changes
        and len(delta_keys) == 1
        and "0:0" not in delta_keys
    ):
        return "UniformLineOffsetOnly"
    if (
        counts_equal
        and no_column_changes
        and len(delta_keys) <= 3
    ):
        return "StableStructureWithLocalizedOffset"
    if (
        not counts_equal
        and no_column_changes
        and first_line_change is not None
        and int(first_line_change["pair_index"]) > 0
    ):
        return "LineRecordCountReductionWithTailRealignment"
    if (
        matching_summary["matched_ratio_against_base"] >= 0.95
        and matching_summary["matched_ratio_against_target"] >= 0.95
    ):
        return "LocalizedSourceChangeWithStableStructure"
    return "NonTrivialTailDrift"


def build_method_row(base_payload: dict[str, Any], target_payload: dict[str, Any], method_name: str) -> dict[str, Any]:
    base_rows = rows_for_method(base_payload, method_name)
    target_rows = rows_for_method(target_payload, method_name)
    delta_summary = summarize_deltas(base_rows, target_rows)
    matching_summary = longest_matching_blocks(base_rows, target_rows)
    return {
        "method_name": method_name,
        "base_count": len(base_rows),
        "target_count": len(target_rows),
        "classification": classify_method(base_rows, target_rows, delta_summary, matching_summary),
        "base_first_rows": [key_for_row(row).__dict__ for row in base_rows[:8]],
        "target_first_rows": [key_for_row(row).__dict__ for row in target_rows[:8]],
        "base_last_rows": [key_for_row(row).__dict__ for row in base_rows[-8:]],
        "target_last_rows": [key_for_row(row).__dict__ for row in target_rows[-8:]],
        "base_only_trailing_rows": [key_for_row(row).__dict__ for row in base_rows[len(target_rows):]],
        "target_only_trailing_rows": [key_for_row(row).__dict__ for row in target_rows[len(base_rows):]],
        "pairwise_delta_summary": delta_summary,
        "sequence_match_summary": matching_summary,
    }


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Compare Service1 lifecycle-tail raw DIA sequence points across two recovered versions."
    )
    parser.add_argument("--base-json", required=True, help="Base version full Service1 sequence-point JSON.")
    parser.add_argument("--target-json", required=True, help="Target version full Service1 sequence-point JSON.")
    parser.add_argument("--base-source", required=True, help="Base version recovered Service1.cs path.")
    parser.add_argument("--target-source", required=True, help="Target version recovered Service1.cs path.")
    parser.add_argument("--output", required=True, help="Path to write comparison JSON.")
    args = parser.parse_args()

    base_json_path = Path(args.base_json).resolve()
    target_json_path = Path(args.target_json).resolve()
    base_source_path = Path(args.base_source).resolve()
    target_source_path = Path(args.target_source).resolve()
    output_path = Path(args.output).resolve()

    base_payload = load_json(base_json_path)
    target_payload = load_json(target_json_path)

    method_rows = [
        build_method_row(base_payload=base_payload, target_payload=target_payload, method_name=method_name)
        for method_name in TARGET_METHODS
    ]

    payload = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "base_version": "2.35",
        "target_version": "2.36",
        "inputs": {
            "base_json": str(base_json_path),
            "target_json": str(target_json_path),
            "base_source": str(base_source_path),
            "target_source": str(target_source_path),
        },
        "target_current_source_positions": find_line_numbers(target_source_path, TARGET_SOURCE_MARKERS),
        "base_current_source_positions": find_line_numbers(base_source_path, BASE_SOURCE_MARKERS),
        "methods": method_rows,
    }

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
