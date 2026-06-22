#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import math
import struct
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


MSF_MAGIC = b"Microsoft C/C++ MSF 7.00\r\n\x1aDS\x00\x00\x00"


def guid_le(raw: bytes) -> str:
    return str(uuid.UUID(bytes_le=raw))


def parse_pdb_identifier(path: Path) -> dict[str, Any]:
    data = path.read_bytes()
    result: dict[str, Any] = {
        "path": str(path.resolve()),
        "kind": "pdb",
        "exists": path.exists(),
        "size": len(data),
        "format": None,
        "guid": None,
        "age": None,
        "error": None,
    }
    try:
        if not data.startswith(MSF_MAGIC):
            result["format"] = "unsupported"
            result["error"] = "not classic MSF 7.00 PDB"
            return result

        block_size, _, _, num_directory_bytes, _, block_map_address = struct.unpack_from("<6I", data, 32)
        directory_block_count = math.ceil(num_directory_bytes / block_size)
        directory_blocks = [
            struct.unpack_from("<I", data, block_map_address * block_size + index * 4)[0]
            for index in range(directory_block_count)
        ]
        directory = b"".join(
            data[block * block_size : (block + 1) * block_size]
            for block in directory_blocks
        )[:num_directory_bytes]

        num_streams = struct.unpack_from("<I", directory, 0)[0]
        if num_streams < 2:
            result["format"] = "classic-msf"
            result["error"] = "missing stream 1"
            return result

        sizes = [struct.unpack_from("<I", directory, 4 + index * 4)[0] for index in range(num_streams)]
        offset = 4 + 4 * num_streams
        stream_blocks: list[list[int]] = []
        for size in sizes:
            if size == 0xFFFFFFFF:
                stream_blocks.append([])
                continue
            block_count = math.ceil(size / block_size)
            blocks = [struct.unpack_from("<I", directory, offset + index * 4)[0] for index in range(block_count)]
            offset += 4 * block_count
            stream_blocks.append(blocks)

        stream1 = b"".join(
            data[block * block_size : (block + 1) * block_size]
            for block in stream_blocks[1]
        )[: sizes[1]]
        if len(stream1) < 28:
            result["format"] = "classic-msf"
            result["error"] = "stream 1 too short"
            return result

        version, signature, age = struct.unpack_from("<III", stream1, 0)
        result.update(
            {
                "format": "classic-msf",
                "version": version,
                "signature": signature,
                "age": age,
                "guid": guid_le(stream1[12:28]),
            }
        )
        return result
    except Exception as exc:
        result["error"] = f"{type(exc).__name__}: {exc}"
        return result


def parse_sections(data: bytes, pe_offset: int, optional_size: int, section_count: int) -> list[dict[str, Any]]:
    section_offset = pe_offset + 4 + 20 + optional_size
    sections: list[dict[str, Any]] = []
    for index in range(section_count):
        offset = section_offset + index * 40
        raw_name = data[offset : offset + 8].split(b"\x00", 1)[0]
        virtual_size, virtual_address, size_of_raw_data, pointer_to_raw_data = struct.unpack_from("<IIII", data, offset + 8)
        sections.append(
            {
                "name": raw_name.decode("ascii", "replace"),
                "virtual_size": virtual_size,
                "virtual_address": virtual_address,
                "size_of_raw_data": size_of_raw_data,
                "pointer_to_raw_data": pointer_to_raw_data,
            }
        )
    return sections


def rva_to_offset(rva: int, sections: list[dict[str, Any]]) -> int | None:
    for section in sections:
        start = section["virtual_address"]
        span = max(section["virtual_size"], section["size_of_raw_data"])
        if start <= rva < start + span:
            return section["pointer_to_raw_data"] + (rva - start)
    return None


def parse_exe_codeview(path: Path) -> dict[str, Any]:
    data = path.read_bytes()
    result: dict[str, Any] = {
        "path": str(path.resolve()),
        "kind": "pe",
        "exists": path.exists(),
        "size": len(data),
        "codeview": [],
        "error": None,
    }
    try:
        if data[:2] != b"MZ":
            result["error"] = "missing MZ signature"
            return result
        pe_offset = struct.unpack_from("<I", data, 0x3C)[0]
        if data[pe_offset : pe_offset + 4] != b"PE\x00\x00":
            result["error"] = "missing PE signature"
            return result

        _, section_count, _, _, _, optional_size, _ = struct.unpack_from("<HHIIIHH", data, pe_offset + 4)
        optional_offset = pe_offset + 4 + 20
        magic = struct.unpack_from("<H", data, optional_offset)[0]
        if magic == 0x10B:
            data_directory_offset = optional_offset + 96
        elif magic == 0x20B:
            data_directory_offset = optional_offset + 112
        else:
            result["error"] = f"unsupported optional header magic 0x{magic:04x}"
            return result

        debug_rva, debug_size = struct.unpack_from("<II", data, data_directory_offset + 6 * 8)
        result["debug_directory_rva"] = debug_rva
        result["debug_directory_size"] = debug_size
        if debug_rva == 0 or debug_size == 0:
            return result

        sections = parse_sections(data, pe_offset, optional_size, section_count)
        debug_offset = rva_to_offset(debug_rva, sections)
        if debug_offset is None:
            result["error"] = "debug directory RVA not mapped to file offset"
            return result

        for index in range(debug_size // 28):
            entry_offset = debug_offset + index * 28
            _, _, _, _, debug_type, size_of_data, address_of_raw_data, pointer_to_raw_data = struct.unpack_from(
                "<IIHHIIII", data, entry_offset
            )
            if debug_type != 2:
                continue
            payload = data[pointer_to_raw_data : pointer_to_raw_data + size_of_data]
            if payload.startswith(b"RSDS") and len(payload) >= 24:
                pdb_path = payload[24:].split(b"\x00", 1)[0].decode("utf-8", "replace")
                result["codeview"].append(
                    {
                        "format": "RSDS",
                        "guid": guid_le(payload[4:20]),
                        "age": struct.unpack_from("<I", payload, 20)[0],
                        "pdb_path": pdb_path,
                    }
                )
            elif payload.startswith(b"NB10") and len(payload) >= 16:
                result["codeview"].append(
                    {
                        "format": "NB10",
                        "signature": struct.unpack_from("<I", payload, 8)[0],
                        "age": struct.unpack_from("<I", payload, 12)[0],
                        "pdb_path": payload[16:].split(b"\x00", 1)[0].decode("utf-8", "replace"),
                    }
                )
        return result
    except Exception as exc:
        result["error"] = f"{type(exc).__name__}: {exc}"
        return result


def iter_scan_paths(root: Path) -> tuple[list[Path], list[Path]]:
    excluded_parts = {".git", "工具"}
    exes: list[Path] = []
    pdbs: list[Path] = []
    for path in root.rglob("*"):
        if not path.is_file():
            continue
        if any(part in excluded_parts for part in path.parts):
            continue
        suffix = path.suffix.lower()
        if suffix == ".exe":
            exes.append(path)
        elif suffix == ".pdb":
            pdbs.append(path)
    return sorted(exes), sorted(pdbs)


def build_matches(executables: list[dict[str, Any]], pdbs: list[dict[str, Any]]) -> list[dict[str, Any]]:
    pdb_index: dict[tuple[str, int], list[dict[str, Any]]] = {}
    for pdb in pdbs:
        guid = pdb.get("guid")
        age = pdb.get("age")
        if guid is None or age is None:
            continue
        pdb_index.setdefault((str(guid).lower(), int(age)), []).append(pdb)

    matches: list[dict[str, Any]] = []
    for exe in executables:
        for codeview in exe.get("codeview", []):
            guid = codeview.get("guid")
            age = codeview.get("age")
            if guid is None or age is None:
                continue
            candidates = pdb_index.get((str(guid).lower(), int(age)), [])
            matches.append(
                {
                    "exe_path": exe["path"],
                    "codeview_guid": guid,
                    "codeview_age": age,
                    "codeview_pdb_path": codeview.get("pdb_path"),
                    "matching_pdb_paths": [candidate["path"] for candidate in candidates],
                    "matching_pdb_count": len(candidates),
                }
            )
    return matches


def main() -> None:
    parser = argparse.ArgumentParser(description="Extract PE CodeView and classic PDB identifiers.")
    parser.add_argument("--scan-root", help="Root to recursively scan for .exe and .pdb files.")
    parser.add_argument("--exe", action="append", default=[], help="Executable path. Can be passed multiple times.")
    parser.add_argument("--pdb", action="append", default=[], help="PDB path. Can be passed multiple times.")
    parser.add_argument("--output", required=True, help="Path to write JSON output.")
    args = parser.parse_args()

    exe_paths = [Path(value) for value in args.exe]
    pdb_paths = [Path(value) for value in args.pdb]
    if args.scan_root:
        scanned_exes, scanned_pdbs = iter_scan_paths(Path(args.scan_root))
        exe_paths.extend(scanned_exes)
        pdb_paths.extend(scanned_pdbs)

    executables = [parse_exe_codeview(path) for path in sorted(set(exe_paths))]
    pdbs = [parse_pdb_identifier(path) for path in sorted(set(pdb_paths))]
    payload = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "executables": executables,
        "pdbs": pdbs,
        "matches": build_matches(executables, pdbs),
    }

    output_path = Path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
