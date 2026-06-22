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


def resolve_function(session: Any, rva: int) -> Any | None:
    try:
        return session.findSymbolByRVA(rva, pydia2.dia.SymTagFunction)
    except Exception:
        return None


def source_files_for_compiland(session: Any, compiland: Any) -> list[Any]:
    try:
        return [
            qi(item, pydia2.dia.IDiaSourceFile)
            for item in session.findFile(compiland, None, 0)
        ]
    except Exception:
        return []


def aggregate_entries(session: Any, compilands: list[Any]) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    entries: dict[tuple[Any, ...], dict[str, Any]] = {}
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
            try:
                line_numbers = iter_dia(
                    session.findLines(compiland, source_file),
                    pydia2.dia.IDiaLineNumber,
                )
            except Exception:
                continue

            for line in line_numbers:
                line_number = int(getattr(line, "lineNumber", 0) or 0)
                if line_number in (0, HIDDEN_SEQUENCE_POINT):
                    continue

                line_number_end = int(getattr(line, "lineNumberEnd", line_number) or line_number)
                rva = int(getattr(line, "relativeVirtualAddress", 0) or 0)
                length = int(getattr(line, "length", 0) or 0)
                column_start = int(getattr(line, "columnNumber", 0) or 0)
                column_end = int(getattr(line, "columnNumberEnd", 0) or 0)
                statement = bool(getattr(line, "statement", False))

                function = resolve_function(session, rva)
                function_name = getattr(function, "name", None)
                token = int(getattr(function, "token", 0) or 0)

                lexical_parent_name = None
                if function is not None:
                    try:
                        lexical_parent_name = getattr(function.lexicalParent, "name", None)
                    except Exception:
                        lexical_parent_name = None

                key = (
                    getattr(source_file, "fileName", None),
                    compiland.name,
                    lexical_parent_name,
                    function_name,
                    token,
                )

                if key not in entries:
                    entries[key] = {
                        "source_file": getattr(source_file, "fileName", None),
                        "compiland": compiland.name,
                        "lexical_parent": lexical_parent_name,
                        "method_name": function_name,
                        "token": token,
                        "line_start": line_number,
                        "line_end": line_number_end,
                        "column_start": column_start,
                        "column_end": column_end,
                        "rva_start": rva,
                        "rva_end": rva + length,
                        "statement_count": 1 if statement else 0,
                        "line_record_count": 1,
                        "hidden_sequence_points_skipped": 0,
                    }
                    continue

                entry = entries[key]
                entry["line_start"] = min(entry["line_start"], line_number)
                entry["line_end"] = max(entry["line_end"], line_number_end)
                if column_start:
                    if not entry["column_start"]:
                        entry["column_start"] = column_start
                    else:
                        entry["column_start"] = min(entry["column_start"], column_start)
                entry["column_end"] = max(entry["column_end"], column_end)
                entry["rva_start"] = min(entry["rva_start"], rva)
                entry["rva_end"] = max(entry["rva_end"], rva + length)
                entry["statement_count"] += 1 if statement else 0
                entry["line_record_count"] += 1

    sorted_entries = sorted(
        entries.values(),
        key=lambda item: (
            Path(item["source_file"]).name if item["source_file"] else "",
            item["line_start"],
            item["line_end"],
            item["lexical_parent"] or "",
            item["method_name"] or "",
            item["token"],
        ),
    )
    return source_file_rows, sorted_entries


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Extract classic DIA method-to-source line spans from a shipped PDB."
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
    parser.add_argument("--output", required=True, help="Path to write JSON output.")
    args = parser.parse_args()

    pdb_path = Path(args.pdb).resolve()
    output_path = Path(args.output).resolve()

    source = pydia2.CreateObject(
        pydia2.dia.DiaSource,
        interface=pydia2.dia.IDiaDataSource,
    )
    source.loadDataFromPdb(str(pdb_path))
    session = source.openSession()

    compilands = collect_compilands(
        session=session,
        exact=set(args.compiland),
        prefixes=list(args.compiland_prefix),
    )
    source_file_rows, entries = aggregate_entries(session, compilands)

    payload = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "pdb_path": str(pdb_path),
        "global_scope_name": getattr(session.globalScope, "name", None),
        "compiland_count": len(compilands),
        "method_entry_count": len(entries),
        "source_files_by_compiland": source_file_rows,
        "method_entries": entries,
    }

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
