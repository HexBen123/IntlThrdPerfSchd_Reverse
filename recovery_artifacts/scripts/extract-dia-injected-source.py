#!/usr/bin/env python3
from __future__ import annotations

import argparse
import ctypes
import hashlib
import json
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import pydia2


GET_SOURCE_VTABLE_SLOT = 9

KNOWN_GUIDS = {
    uuid.UUID("3f5162f8-07c6-11d3-9053-00c04fa302a1"): "CorSym_LanguageType_CSharp",
    uuid.UUID("994b45c4-e6e9-11d2-903f-00c04fa302a1"): "CorSym_LanguageVendor_Microsoft",
    uuid.UUID("5a869d0b-6611-11d3-bd2a-0000f80849bd"): "CorSym_DocumentType_Text",
    uuid.UUID("8829d00f-11b8-4213-878b-770e8597ac16"): "SHA256",
}

TABLE_INTERFACES: dict[str, Any] = {
    "SourceFiles": pydia2.dia.IDiaEnumSourceFiles,
    "LineNumbers": pydia2.dia.IDiaEnumLineNumbers,
    "InjectedSource": pydia2.dia.IDiaEnumInjectedSources,
    "InputAssemblyFiles": pydia2.dia.IDiaEnumInputAssemblyFiles,
    "Symbols": pydia2.dia.IDiaEnumSymbols,
}


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


def table_counts(session: Any) -> dict[str, int | None]:
    counts: dict[str, int | None] = {}
    for raw_table in session.getEnumTables():
        table = qi(raw_table, pydia2.dia.IDiaTable)
        name = getattr(table, "name", None)
        if not name:
            continue
        interface = TABLE_INTERFACES.get(name)
        if interface is None:
            counts[name] = None
            continue
        try:
            counts[name] = int(getattr(qi(raw_table, interface), "count", 0) or 0)
        except Exception:
            counts[name] = None
    return counts


def list_compilands(session: Any) -> list[Any]:
    global_scope = session.globalScope
    compilands = [
        qi(item, pydia2.dia.IDiaSymbol)
        for item in global_scope.findChildren(pydia2.dia.SymTagCompiland, None, 0)
    ]
    compilands = [item for item in compilands if getattr(item, "name", None)]
    compilands.sort(key=lambda item: item.name)
    return compilands


def find_injected_sources(session: Any) -> list[Any]:
    for raw_table in session.getEnumTables():
        table = qi(raw_table, pydia2.dia.IDiaTable)
        if getattr(table, "name", None) != "InjectedSource":
            continue
        enum_obj = qi(raw_table, pydia2.dia.IDiaEnumInjectedSources)
        return [
            qi(item, pydia2.dia.IDiaInjectedSource)
            for item in enum_obj
        ]
    return []


def guid_from_bytes_le(payload: bytes, offset: int) -> str | None:
    if len(payload) < offset + 16:
        return None
    return str(uuid.UUID(bytes_le=payload[offset : offset + 16]))


def raw_get_source_payload(injected_source: Any) -> bytes:
    length = int(getattr(injected_source, "length", 0) or 0)
    vtable = ctypes.cast(
        injected_source,
        ctypes.POINTER(ctypes.POINTER(ctypes.c_void_p)),
    ).contents
    method_address = int(vtable[GET_SOURCE_VTABLE_SLOT])
    prototype = ctypes.WINFUNCTYPE(
        ctypes.c_long,
        ctypes.c_void_p,
        ctypes.c_ulong,
        ctypes.POINTER(ctypes.c_ulong),
        ctypes.POINTER(ctypes.c_ubyte),
    )
    method = prototype(method_address)
    actual_size = ctypes.c_ulong(0)
    buffer = (ctypes.c_ubyte * length)()
    hr = method(
        ctypes.cast(injected_source, ctypes.c_void_p),
        length,
        ctypes.byref(actual_size),
        buffer,
    )
    if hr != 0:
        raise OSError(f"IDiaInjectedSource::get_source failed with HRESULT 0x{ctypes.c_ulong(hr).value:08X}")
    return bytes(buffer[: actual_size.value])


def parse_payload(payload: bytes) -> dict[str, Any]:
    result: dict[str, Any] = {
        "payload_length": len(payload),
        "payload_hex": payload.hex(),
        "payload_sha256": hashlib.sha256(payload).hexdigest(),
        "payload_first_32_hex": payload[:32].hex(),
    }

    parsed_guids: list[dict[str, Any]] = []
    for offset, field_name in (
        (0, "language_guid"),
        (16, "vendor_guid"),
        (32, "document_type_guid"),
        (48, "checksum_algorithm_guid"),
    ):
        guid_text = guid_from_bytes_le(payload, offset)
        if guid_text is None:
            continue
        guid_obj = uuid.UUID(guid_text)
        parsed_guids.append(
            {
                "offset": offset,
                "field": field_name,
                "guid": guid_text,
                "known_name": KNOWN_GUIDS.get(guid_obj),
            }
        )
        result[field_name] = guid_text
        if guid_obj in KNOWN_GUIDS:
            result[f"{field_name}_name"] = KNOWN_GUIDS[guid_obj]

    result["parsed_guids"] = parsed_guids

    if len(payload) >= 72:
        checksum_length = int.from_bytes(payload[64:68], "little")
        reserved_dword = int.from_bytes(payload[68:72], "little")
        checksum_bytes = payload[72 : 72 + checksum_length]
        result["checksum_length_le_uint32"] = checksum_length
        result["reserved_dword_le_uint32"] = reserved_dword
        result["embedded_checksum_hex"] = checksum_bytes.hex()
        result["suffix_hex_from_offset_64"] = payload[64:].hex()

    return result


def map_recovered_path(recovered_root: Path, file_name: str) -> Path:
    name = Path(file_name).name
    if name == ".NETFramework,Version=v4.8.AssemblyAttributes.cs":
        return recovered_root / "obj" / "Release" / "net48" / name
    return recovered_root / name


def enrich_recovered_hash(row: dict[str, Any], recovered_root: Path | None) -> None:
    if recovered_root is None:
        return
    recovered_path = map_recovered_path(recovered_root, row["file_name"])
    row["recovered_candidate_path"] = str(recovered_path)
    row["recovered_exists"] = recovered_path.exists()
    if not recovered_path.exists():
        row["recovered_sha256"] = None
        row["recovered_sha256_matches_embedded_checksum"] = None
        return

    recovered_sha256 = hashlib.sha256(recovered_path.read_bytes()).hexdigest()
    row["recovered_sha256"] = recovered_sha256
    embedded_checksum = row.get("embedded_checksum_hex")
    if embedded_checksum:
        row["recovered_sha256_matches_embedded_checksum"] = (
            recovered_sha256.lower() == embedded_checksum.lower()
        )
    else:
        row["recovered_sha256_matches_embedded_checksum"] = None


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Extract classic DIA InjectedSource entries and raw payload bytes from a shipped PDB."
    )
    parser.add_argument("--pdb", required=True, help="Path to the classic PDB file.")
    parser.add_argument("--output", required=True, help="Path to write JSON output.")
    parser.add_argument(
        "--recovered-root",
        help="Optional recovered source root for SHA-256 comparison against embedded checksum data.",
    )
    args = parser.parse_args()

    pdb_path = Path(args.pdb).resolve()
    output_path = Path(args.output).resolve()
    recovered_root = Path(args.recovered_root).resolve() if args.recovered_root else None

    session = load_session(pdb_path)
    counts = table_counts(session)
    compilands = list_compilands(session)
    injected_sources = find_injected_sources(session)

    rows: list[dict[str, Any]] = []
    for injected_source in injected_sources:
        payload = raw_get_source_payload(injected_source)
        row = {
            "file_name": getattr(injected_source, "fileName", None),
            "virtual_filename": getattr(injected_source, "virtualFilename", None),
            "object_file_name": getattr(injected_source, "objectFileName", None),
            "crc": int(getattr(injected_source, "crc", 0) or 0),
            "length": int(getattr(injected_source, "length", 0) or 0),
            "source_compression": int(getattr(injected_source, "sourceCompression", 0) or 0),
            "comtypes_get_source_zero_probe": tuple(int(value) for value in injected_source.get_source(0)),
            "comtypes_get_source_one_probe": tuple(int(value) for value in injected_source.get_source(1)),
        }
        row.update(parse_payload(payload))
        enrich_recovered_hash(row, recovered_root)
        rows.append(row)

    recovered_match_count = sum(
        1
        for row in rows
        if row.get("recovered_sha256_matches_embedded_checksum") is True
    )

    payload_sha256_unique = len({row["payload_sha256"] for row in rows})
    payload_prefix_unique = len({row["payload_first_32_hex"] for row in rows})

    payload = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "pdb_path": str(pdb_path),
        "recovered_root": str(recovered_root) if recovered_root is not None else None,
        "global_scope_name": getattr(session.globalScope, "name", None),
        "table_counts": counts,
        "all_compiland_count": len(compilands),
        "special_compilands": [
            compiland.name
            for compiland in compilands
            if compiland.name.startswith("*") or compiland.name.startswith("<")
        ],
        "marshal_observation": {
            "comtypes_wrapper_issue": (
                "IDiaInjectedSource.get_source() 被 comtypes 生成为 (pcbData, pbData)；"
                "其中 pbData 仅被自动解组为单个 BYTE，不能直接拿到完整缓冲区。"
            ),
            "raw_vtable_slot_used": GET_SOURCE_VTABLE_SLOT,
        },
        "payload_observations": {
            "row_count": len(rows),
            "unique_payload_sha256_count": payload_sha256_unique,
            "unique_first_32_hex_count": payload_prefix_unique,
            "all_first_32_hex_identical": payload_prefix_unique == 1,
            "recovered_sha256_match_count": recovered_match_count,
        },
        "injected_sources": rows,
    }

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
