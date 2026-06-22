#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import pydia2


HIDDEN_SEQUENCE_POINT = 0xFEEFEE


def qi(obj: Any, interface: Any) -> Any:
    try:
        return obj.QueryInterface(interface)
    except Exception:
        return obj


def iter_dia(enum_obj: Any, interface: Any | None = None) -> list[Any]:
    items: list[Any] = []
    if enum_obj is None:
        return items
    for item in enum_obj:
        if interface is not None:
            item = qi(item, interface)
        items.append(item)
    return items


def load_session(pdb_path: Path) -> Any:
    source = pydia2.CreateObject(
        pydia2.dia.DiaSource,
        interface=pydia2.dia.IDiaDataSource,
    )
    source.loadDataFromPdb(str(pdb_path))
    return source.openSession()


def collect_compilands(session: Any, exact: set[str], prefixes: list[str]) -> list[Any]:
    global_scope = session.globalScope
    compilands = [
        qi(item, pydia2.dia.IDiaSymbol)
        for item in global_scope.findChildren(pydia2.dia.SymTagCompiland, None, 0)
    ]
    filtered: list[Any] = []
    for compiland in compilands:
        name = getattr(compiland, "name", None)
        if not name:
            continue
        if exact and name in exact:
            filtered.append(compiland)
            continue
        if prefixes and any(name.startswith(prefix) for prefix in prefixes):
            filtered.append(compiland)
    filtered.sort(key=lambda item: item.name)
    return filtered


def source_files_for_compiland(session: Any, compiland: Any) -> list[Any]:
    try:
        return [
            qi(item, pydia2.dia.IDiaSourceFile)
            for item in session.findFile(compiland, None, 0)
        ]
    except Exception:
        return []


def resolve_function(session: Any, rva: int) -> Any | None:
    try:
        return session.findSymbolByRVA(rva, pydia2.dia.SymTagFunction)
    except Exception:
        return None


def build_rows(
    session: Any,
    compilands: list[Any],
    source_name_filters: set[str],
    include_hidden: bool,
) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    rows: list[dict[str, Any]] = []
    source_file_rows: list[dict[str, Any]] = []

    for compiland in compilands:
        source_files = source_files_for_compiland(session, compiland)
        source_file_rows.append(
            {
                "compiland": compiland.name,
                "source_files": [getattr(source_file, "fileName", None) for source_file in source_files],
            }
        )
        for source_file in source_files:
            source_file_name = getattr(source_file, "fileName", None)
            source_base_name = Path(source_file_name).name if source_file_name else None
            if source_name_filters and source_base_name not in source_name_filters:
                continue

            try:
                line_numbers = iter_dia(
                    session.findLines(compiland, source_file),
                    pydia2.dia.IDiaLineNumber,
                )
            except Exception:
                continue

            for line in line_numbers:
                line_number = int(getattr(line, "lineNumber", 0) or 0)
                if not include_hidden and line_number in (0, HIDDEN_SEQUENCE_POINT):
                    continue

                line_number_end = int(getattr(line, "lineNumberEnd", line_number) or line_number)
                rva = int(getattr(line, "relativeVirtualAddress", 0) or 0)
                length = int(getattr(line, "length", 0) or 0)
                function = resolve_function(session, rva)
                function_name = getattr(function, "name", None)
                token = int(getattr(function, "token", 0) or 0)
                lexical_parent_name = None
                if function is not None:
                    try:
                        lexical_parent_name = getattr(function.lexicalParent, "name", None)
                    except Exception:
                        lexical_parent_name = None

                row = {
                    "source_file": source_file_name,
                    "source_file_base_name": source_base_name,
                    "compiland": compiland.name,
                    "lexical_parent": lexical_parent_name,
                    "method_name": function_name,
                    "token": token,
                    "line": line_number,
                    "line_end": line_number_end,
                    "column_start": int(getattr(line, "columnNumber", 0) or 0),
                    "column_end": int(getattr(line, "columnNumberEnd", 0) or 0),
                    "relative_virtual_address": rva,
                    "length": length,
                    "statement": bool(getattr(line, "statement", False)),
                    "is_hidden_sequence_point": line_number == HIDDEN_SEQUENCE_POINT,
                    "is_zero_line": line_number == 0,
                }
                rows.append(row)

    rows.sort(
        key=lambda item: (
            item["source_file_base_name"] or "",
            item["line"],
            item["line_end"],
            item["column_start"],
            item["relative_virtual_address"],
        )
    )
    return source_file_rows, rows


def summarize_rows(rows: list[dict[str, Any]]) -> list[dict[str, Any]]:
    by_file: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for row in rows:
        by_file[row["source_file"]].append(row)

    summaries: list[dict[str, Any]] = []
    for source_file, file_rows in sorted(by_file.items(), key=lambda item: Path(item[0]).name):
        visible_rows = [
            row for row in file_rows
            if not row["is_hidden_sequence_point"] and not row["is_zero_line"]
        ]
        method_names = []
        seen_methods: set[tuple[str | None, int]] = set()
        for row in visible_rows:
            key = (row["method_name"], row["token"])
            if key in seen_methods:
                continue
            seen_methods.add(key)
            method_names.append(
                {
                    "method_name": row["method_name"],
                    "token": row["token"],
                }
            )
        summaries.append(
            {
                "source_file": source_file,
                "source_file_base_name": Path(source_file).name,
                "visible_record_count": len(visible_rows),
                "visible_lines": [row["line"] for row in visible_rows],
                "method_count": len(method_names),
                "methods": method_names,
            }
        )
    return summaries


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Extract raw DIA line and column sequence-point records from a shipped PDB."
    )
    parser.add_argument("--pdb", required=True, help="Path to the classic PDB file.")
    parser.add_argument(
        "--compiland-prefix",
        action="append",
        default=[],
        help="Filter compilands by prefix. Can be passed multiple times.",
    )
    parser.add_argument(
        "--compiland",
        action="append",
        default=[],
        help="Filter compilands by exact name. Can be passed multiple times.",
    )
    parser.add_argument(
        "--source-name",
        action="append",
        default=[],
        help="Filter by source file base name. Can be passed multiple times.",
    )
    parser.add_argument(
        "--include-hidden",
        action="store_true",
        help="Include zero-line and hidden sequence points in the output.",
    )
    parser.add_argument("--output", required=True, help="Path to write JSON output.")
    args = parser.parse_args()

    pdb_path = Path(args.pdb).resolve()
    output_path = Path(args.output).resolve()

    session = load_session(pdb_path)
    compilands = collect_compilands(
        session=session,
        exact=set(args.compiland),
        prefixes=list(args.compiland_prefix),
    )
    source_file_rows, rows = build_rows(
        session=session,
        compilands=compilands,
        source_name_filters=set(args.source_name),
        include_hidden=args.include_hidden,
    )
    payload = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "pdb_path": str(pdb_path),
        "global_scope_name": getattr(session.globalScope, "name", None),
        "compiland_count": len(compilands),
        "sequence_point_count": len(rows),
        "filters": {
            "compiland": list(args.compiland),
            "compiland_prefix": list(args.compiland_prefix),
            "source_name": list(args.source_name),
            "include_hidden": bool(args.include_hidden),
        },
        "source_files_by_compiland": source_file_rows,
        "source_file_summaries": summarize_rows(rows),
        "sequence_points": rows,
    }

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
