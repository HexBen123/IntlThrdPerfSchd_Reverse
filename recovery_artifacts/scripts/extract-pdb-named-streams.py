import argparse
import hashlib
import json
import math
import struct
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


MSF_MAGIC = b"Microsoft C/C++ MSF 7.00\r\n\x1aDS\x00\x00\x00"
NAMES_STREAM_SIGNATURE = 0xEFFEEFFE
SRC_HEADERBLOCK_VERSION = 0x0130E21B


def parse_superblock(data: bytes) -> dict[str, int]:
    if not data.startswith(MSF_MAGIC):
        raise ValueError("Unsupported PDB/MSF magic header.")

    (
        block_size,
        free_block_map_block,
        num_blocks,
        num_directory_bytes,
        reserved_block_map_block,
        block_map_address,
    ) = struct.unpack_from("<6I", data, 32)

    return {
        "block_size": block_size,
        "free_block_map_block": free_block_map_block,
        "num_blocks": num_blocks,
        "num_directory_bytes": num_directory_bytes,
        "reserved_block_map_block": reserved_block_map_block,
        "block_map_address": block_map_address,
    }


def read_stream_bytes(data: bytes, block_size: int, blocks: list[int], size: int) -> bytes:
    return b"".join(data[block * block_size : (block + 1) * block_size] for block in blocks)[:size]


def parse_directory(data: bytes, superblock: dict[str, int]) -> dict[str, Any]:
    block_size = superblock["block_size"]
    num_directory_bytes = superblock["num_directory_bytes"]
    block_map_address = superblock["block_map_address"]
    directory_block_count = math.ceil(num_directory_bytes / block_size)
    directory_blocks = [
        struct.unpack_from("<I", data, block_map_address * block_size + index * 4)[0]
        for index in range(directory_block_count)
    ]
    directory_bytes = read_stream_bytes(data, block_size, directory_blocks, num_directory_bytes)

    num_streams = struct.unpack_from("<I", directory_bytes, 0)[0]
    sizes = [struct.unpack_from("<I", directory_bytes, 4 + index * 4)[0] for index in range(num_streams)]
    offset = 4 + 4 * num_streams

    stream_blocks: list[list[int]] = []
    for size in sizes:
        if size == 0xFFFFFFFF:
            stream_blocks.append([])
            continue

        block_count = math.ceil(size / block_size)
        blocks = [struct.unpack_from("<I", directory_bytes, offset + index * 4)[0] for index in range(block_count)]
        offset += 4 * block_count
        stream_blocks.append(blocks)

    return {
        "directory_blocks": directory_blocks,
        "directory_bytes_length": len(directory_bytes),
        "num_streams": num_streams,
        "stream_sizes": sizes,
        "stream_blocks": stream_blocks,
    }


def guid_le_bytes_to_string(raw: bytes) -> str:
    data1, data2, data3 = struct.unpack_from("<IHH", raw, 0)
    data4 = raw[8:]
    return (
        f"{data1:08x}-{data2:04x}-{data3:04x}-"
        f"{data4[0]:02x}{data4[1]:02x}-"
        f"{data4[2]:02x}{data4[3]:02x}{data4[4]:02x}{data4[5]:02x}{data4[6]:02x}{data4[7]:02x}"
    )


def parse_sparse_bit_vector(buffer: bytes, offset: int) -> tuple[list[int], int]:
    word_count = struct.unpack_from("<I", buffer, offset)[0]
    offset += 4
    words = list(struct.unpack_from("<" + "I" * word_count, buffer, offset))
    offset += 4 * word_count
    buckets: list[int] = []
    for word_index, word in enumerate(words):
        for bit_index in range(32):
            if word & (1 << bit_index):
                buckets.append(word_index * 32 + bit_index)
    return buckets, offset


def parse_names_stream(payload: bytes) -> dict[str, Any]:
    signature, hash_version, byte_size = struct.unpack_from("<III", payload, 0)
    strings_blob = payload[12 : 12 + byte_size]
    hash_count = struct.unpack_from("<I", payload, 12 + byte_size)[0]
    hash_ids_offset = 16 + byte_size
    hash_ids = list(struct.unpack_from("<" + "I" * hash_count, payload, hash_ids_offset))
    name_count = struct.unpack_from("<I", payload, hash_ids_offset + 4 * hash_count)[0]

    strings: list[dict[str, Any]] = []
    string_by_offset: dict[int, str] = {}
    cursor = 0
    while cursor < len(strings_blob):
        string_end = strings_blob.find(b"\x00", cursor)
        if string_end < 0:
            break
        if string_end > cursor:
            text = strings_blob[cursor:string_end].decode("utf-8", "replace")
            strings.append({"offset": cursor, "text": text})
            string_by_offset[cursor] = text
        cursor = string_end + 1

    nonzero_hash_ids = [value for value in hash_ids if value != 0]
    empty_string_ids = sorted({value for value in nonzero_hash_ids if value not in string_by_offset})
    original_case = [item["text"] for item in strings if "\\Users\\maxpp\\" in item["text"]]
    lower_case = [item["text"] for item in strings if item["text"] == item["text"].lower()]
    path_pairs = {
        item["text"].lower(): item["text"]
        for item in strings
        if item["text"] != item["text"].lower()
    }

    return {
        "signature": signature,
        "signature_matches_expected": signature == NAMES_STREAM_SIGNATURE,
        "hash_version": hash_version,
        "byte_size": byte_size,
        "hash_count": hash_count,
        "name_count": name_count,
        "string_count": len(strings),
        "strings": strings,
        "hash_ids": hash_ids,
        "nonzero_hash_id_count": len(nonzero_hash_ids),
        "unique_nonzero_hash_id_count": len(set(nonzero_hash_ids)),
        "empty_string_ids": empty_string_ids,
        "empty_string_id_count": len(empty_string_ids),
        "original_case_path_count": len(original_case),
        "lowercase_path_count": len(lower_case),
        "path_pair_count": len(path_pairs),
        "all_nonempty_strings_look_like_paths": all(":\\" in item["text"] for item in strings),
        "string_by_offset": {str(key): value for key, value in string_by_offset.items()},
    }


def parse_src_headerblock(payload: bytes, names_stream: dict[str, Any]) -> dict[str, Any]:
    version, header_size = struct.unpack_from("<II", payload, 0)
    filetime = struct.unpack_from("<Q", payload, 8)[0]
    age = struct.unpack_from("<I", payload, 16)[0]
    names_by_offset = {int(key): value for key, value in names_stream["string_by_offset"].items()}
    empty_string_ids = {int(value) for value in names_stream["empty_string_ids"]}

    offset = 64
    table_size, table_capacity = struct.unpack_from("<II", payload, offset)
    offset += 8
    present_buckets, offset = parse_sparse_bit_vector(payload, offset)
    deleted_buckets, offset = parse_sparse_bit_vector(payload, offset)

    entries: list[dict[str, Any]] = []
    for bucket in present_buckets:
        key = struct.unpack_from("<I", payload, offset)[0]
        offset += 4
        (
            entry_size,
            entry_version,
            crc,
            file_size,
            file_ni,
            obj_ni,
            virtual_file_ni,
        ) = struct.unpack_from("<IIIIIII", payload, offset)
        offset += 28
        compression = payload[offset]
        is_virtual = payload[offset + 1]
        reserved_padding = struct.unpack_from("<H", payload, offset + 2)[0]
        reserved_bytes = payload[offset + 4 : offset + 12]
        offset += 12

        file_name = names_by_offset.get(file_ni)
        object_name = names_by_offset.get(obj_ni)
        virtual_file_name = names_by_offset.get(virtual_file_ni)
        entries.append(
            {
                "bucket": bucket,
                "key": key,
                "entry_size": entry_size,
                "entry_version": entry_version,
                "entry_version_matches_expected": entry_version == SRC_HEADERBLOCK_VERSION,
                "crc": crc,
                "file_size": file_size,
                "file_name_id": file_ni,
                "object_name_id": obj_ni,
                "virtual_file_name_id": virtual_file_ni,
                "file_name": file_name,
                "object_name": object_name,
                "object_name_is_empty_string_id": obj_ni in empty_string_ids,
                "virtual_file_name": virtual_file_name,
                "compression": compression,
                "is_virtual": is_virtual,
                "reserved_padding": reserved_padding,
                "reserved_bytes_hex": reserved_bytes.hex(),
                "key_equals_virtual_file_name_id": key == virtual_file_ni,
                "file_name_has_original_case": file_name is not None and file_name != file_name.lower(),
                "virtual_file_name_is_lowercase": (
                    virtual_file_name is not None and virtual_file_name == virtual_file_name.lower()
                ),
            }
        )

    return {
        "version": version,
        "version_matches_expected": version == SRC_HEADERBLOCK_VERSION,
        "header_size": header_size,
        "filetime": filetime,
        "age": age,
        "table_size": table_size,
        "table_capacity": table_capacity,
        "present_bucket_count": len(present_buckets),
        "deleted_bucket_count": len(deleted_buckets),
        "present_buckets": present_buckets,
        "deleted_buckets": deleted_buckets,
        "entries": entries,
        "entries_count": len(entries),
        "all_entries_have_size_40": all(item["entry_size"] == 40 for item in entries),
        "all_entries_compression_101": all(item["compression"] == 101 for item in entries),
        "all_entries_not_virtual": all(item["is_virtual"] == 0 for item in entries),
        "all_entries_object_name_empty": all(item["object_name_is_empty_string_id"] for item in entries),
        "all_entries_key_equals_virtual_name_id": all(item["key_equals_virtual_file_name_id"] for item in entries),
        "all_entries_virtual_file_names_lowercase": all(
            item["virtual_file_name_is_lowercase"] for item in entries
        ),
        "all_entries_file_names_original_case": all(item["file_name_has_original_case"] for item in entries),
        "remaining_trailing_bytes_hex": payload[offset:].hex(),
    }


def parse_named_stream_details(
    data: bytes,
    superblock: dict[str, int],
    directory: dict[str, Any],
    compare_injected: dict[str, Any] | None,
) -> tuple[dict[str, Any], list[dict[str, Any]], dict[str, int]]:
    block_size = superblock["block_size"]
    stream_sizes = directory["stream_sizes"]
    stream_blocks = directory["stream_blocks"]
    pdb_stream = read_stream_bytes(data, block_size, stream_blocks[1], stream_sizes[1])

    version, signature, age = struct.unpack_from("<III", pdb_stream, 0)
    guid = guid_le_bytes_to_string(pdb_stream[12:28])
    string_buffer_size = struct.unpack_from("<I", pdb_stream, 28)[0]
    string_buffer = pdb_stream[32 : 32 + string_buffer_size]
    offset = 32 + string_buffer_size

    map_size, map_capacity, present_word_count = struct.unpack_from("<III", pdb_stream, offset)
    offset += 12
    present_words = list(struct.unpack_from("<" + "I" * present_word_count, pdb_stream, offset))
    offset += 4 * present_word_count
    deleted_word_count = struct.unpack_from("<I", pdb_stream, offset)[0]
    offset += 4
    offset += 4 * deleted_word_count

    entries: list[dict[str, Any]] = []
    name_to_stream_index: dict[str, int] = {}
    for _ in range(map_size):
        key_offset, stream_index = struct.unpack_from("<II", pdb_stream, offset)
        offset += 8
        string_end = string_buffer.find(b"\x00", key_offset)
        name = string_buffer[key_offset:string_end].decode("utf-8", "replace")
        name_to_stream_index[name] = stream_index
        payload = read_stream_bytes(data, block_size, stream_blocks[stream_index], stream_sizes[stream_index])
        sha256 = hashlib.sha256(payload).hexdigest()
        row: dict[str, Any] = {
            "name": name,
            "stream_index": stream_index,
            "size": stream_sizes[stream_index],
            "blocks": stream_blocks[stream_index],
            "sha256": sha256,
            "hex_preview_64": payload[:64].hex(),
            "ascii_preview_160": "".join(chr(byte) if 32 <= byte < 127 else "." for byte in payload[:160]),
        }

        if name.startswith("/src/files/"):
            row["kind"] = "src_file_metadata"
            row["source_path_lower"] = name[len("/src/files/") :]
            row["payload_hex"] = payload.hex()
            if compare_injected is not None:
                injected_row = compare_injected.get(row["source_path_lower"])
                if injected_row is not None:
                    row["matches_injected_payload_hex"] = payload.hex() == injected_row["payload_hex"]
                    row["matches_injected_embedded_checksum_hex"] = (
                        hashlib.sha256(payload).hexdigest() == injected_row["payload_sha256"]
                    )
                    row["injected_embedded_checksum_hex"] = injected_row["embedded_checksum_hex"]
                else:
                    row["matches_injected_payload_hex"] = False
                    row["matches_injected_embedded_checksum_hex"] = False
        elif name == "/src/headerblock":
            row["kind"] = "src_headerblock"
        elif name == "/names":
            row["kind"] = "names"
        else:
            row["kind"] = "other_named_stream"

        entries.append(row)

    tail_bytes = pdb_stream[offset:]
    tail_u32s = list(struct.unpack("<" + "I" * (len(tail_bytes) // 4), tail_bytes)) if tail_bytes else []

    metadata = {
        "version": version,
        "signature": signature,
        "age": age,
        "guid": guid,
        "string_buffer_size": string_buffer_size,
        "named_stream_map_size": map_size,
        "named_stream_map_capacity": map_capacity,
        "present_word_count": present_word_count,
        "deleted_word_count": deleted_word_count,
        "trailing_tail_u32s": tail_u32s,
    }
    return metadata, entries, name_to_stream_index


def load_injected_lookup(path: Path | None) -> dict[str, Any] | None:
    if path is None:
        return None

    payload = json.loads(path.read_text(encoding="utf-8"))
    result: dict[str, Any] = {}
    for row in payload.get("injected_sources", []):
        file_name = str(row.get("file_name", "")).lower()
        result[file_name] = row
    return result


def build_summary(named_streams: list[dict[str, Any]]) -> dict[str, Any]:
    src_file_streams = [row for row in named_streams if row["kind"] == "src_file_metadata"]
    zero_length_streams = [row["name"] for row in named_streams if row["size"] == 0]
    src_sizes = sorted({row["size"] for row in src_file_streams})
    injected_matches = [
        row for row in src_file_streams if row.get("matches_injected_payload_hex") is True
    ]

    return {
        "named_stream_count": len(named_streams),
        "source_file_named_stream_count": len(src_file_streams),
        "zero_length_named_stream_count": len(zero_length_streams),
        "zero_length_named_streams": zero_length_streams,
        "source_file_named_stream_unique_sizes": src_sizes,
        "all_source_file_named_streams_same_size": len(src_sizes) == 1,
        "source_file_named_streams_match_injected_payload_count": len(injected_matches),
        "all_source_file_named_streams_match_injected_payload": (
            len(src_file_streams) > 0 and len(injected_matches) == len(src_file_streams)
        ),
    }


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Extract raw named stream map evidence from a classic MSF/PDB file."
    )
    parser.add_argument("--pdb", required=True, help="Path to the PDB file.")
    parser.add_argument("--output", required=True, help="Path to write JSON output.")
    parser.add_argument(
        "--compare-injected-json",
        help="Optional dia_injected_source_*.json path for payload equality checks.",
    )
    args = parser.parse_args()

    pdb_path = Path(args.pdb).resolve()
    output_path = Path(args.output).resolve()
    compare_injected_path = Path(args.compare_injected_json).resolve() if args.compare_injected_json else None

    data = pdb_path.read_bytes()
    superblock = parse_superblock(data)
    directory = parse_directory(data, superblock)
    compare_injected = load_injected_lookup(compare_injected_path)
    pdb_stream_metadata, named_streams, named_stream_index = parse_named_stream_details(
        data=data,
        superblock=superblock,
        directory=directory,
        compare_injected=compare_injected,
    )
    block_size = superblock["block_size"]
    stream_sizes = directory["stream_sizes"]
    stream_blocks = directory["stream_blocks"]
    names_stream = parse_names_stream(
        read_stream_bytes(
            data,
            block_size,
            stream_blocks[named_stream_index["/names"]],
            stream_sizes[named_stream_index["/names"]],
        )
    )
    src_headerblock = parse_src_headerblock(
        read_stream_bytes(
            data,
            block_size,
            stream_blocks[named_stream_index["/src/headerblock"]],
            stream_sizes[named_stream_index["/src/headerblock"]],
        ),
        names_stream=names_stream,
    )
    payload = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "pdb_path": str(pdb_path),
        "compare_injected_json": str(compare_injected_path) if compare_injected_path else None,
        "superblock": superblock,
        "directory": {
            "directory_blocks": directory["directory_blocks"],
            "directory_bytes_length": directory["directory_bytes_length"],
            "num_streams": directory["num_streams"],
        },
        "pdb_stream": pdb_stream_metadata,
        "names_stream": names_stream,
        "src_headerblock": src_headerblock,
        "summary": build_summary(named_streams),
        "named_streams": named_streams,
    }

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
