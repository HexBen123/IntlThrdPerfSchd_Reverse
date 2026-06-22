import hashlib
import pathlib
import re


def main() -> None:
    repo_root = pathlib.Path(__file__).resolve().parents[2]
    exe_path = (
        repo_root
        / "Intel大小核神经网络调度器N版2.51永久权重（20分钟训练版）"
        / "IntlThrdSchd"
        / "IntlThrdSchd.exe"
    )
    out_path = pathlib.Path(__file__).resolve().with_name("binary_string_scan_2.51.txt")

    data = exe_path.read_bytes()
    sha256 = hashlib.sha256(data).hexdigest()

    lines: list[str] = []
    lines.append(f"path: {exe_path}")
    lines.append(f"size: {len(data)}")
    lines.append(f"sha256: {sha256}")
    lines.append("")

    for sig in [b"RSDS", b"NB10", b"PDB:", b".pdb", b".PDB", b".cs", b".sln", b".csproj"]:
        lines.append(f"find {sig!r}: {data.find(sig)}")
    lines.append("")

    strings = re.findall(rb"[\x20-\x7E]{4,}", data)
    lines.append(f"ascii_strings_count(len>=4): {len(strings)}")

    # Avoid overmatching on raw "\" which is common in random binary data.
    keywords = [
        b".cs",
        b"source",
        b"repos",
        b".sln",
        b".csproj",
        b"IntlThrd",
        b"PerfSchd",
        b".pdb",
    ]
    interesting: list[bytes] = []
    for s in strings:
        ls = s.lower()
        if any(k.lower() in ls for k in keywords):
            interesting.append(s)

    lines.append(f"interesting_ascii_strings_count: {len(interesting)}")
    lines.append("")
    for s in interesting:
        lines.append(s.decode("utf-8", errors="replace"))
    lines.append("")

    # UTF-16LE "wide" strings occasionally contain paths even when ASCII does not.
    wide_strings = re.findall(rb"(?:[\x20-\x7E]\x00){4,}", data)
    lines.append(f"utf16le_strings_count(len>=4): {len(wide_strings)}")
    wide_interesting: list[bytes] = []
    for s in wide_strings:
        # Convert to a comparable ASCII-ish byte sequence for keyword filtering.
        decoded = s.decode("utf-16le", errors="replace").encode("utf-8", errors="replace")
        ls = decoded.lower()
        if any(k.lower() in ls for k in keywords):
            wide_interesting.append(decoded)
    lines.append(f"interesting_utf16le_strings_count: {len(wide_interesting)}")
    lines.append("")
    for s in wide_interesting:
        lines.append(s.decode("utf-8", errors="replace"))
    lines.append("")

    out_path.write_text("\n".join(lines), encoding="utf-8")
    print(f"wrote: {out_path}")


if __name__ == "__main__":
    main()
