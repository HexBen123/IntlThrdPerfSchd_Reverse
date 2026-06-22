#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
from dataclasses import dataclass
from datetime import datetime, timezone
from itertools import product
from pathlib import Path
from typing import Iterable


TARGETS = {
    "ProjectInstaller.cs": "5eaf364e732ef11197449d06b1c09aa3cf61509b52cc2823b8c2880741455b9d",
    "ProjectInstaller.Designer.cs": "6013fffa85739d2796c81344ef609335bc21701eb69b3a85413c04e238e16140",
    "Service1.Designer.cs": "55ebaa1a84f3b504cbb7d938d18cceca024495279e7904d47ee17c1a20e30b6b",
}


@dataclass(frozen=True)
class Candidate:
    file_name: str
    variant_name: str
    text: str
    metadata: dict[str, str]


def encode_text(text: str, newline: str, bom: bool) -> bytes:
    normalized = text.replace("\r\n", "\n").replace("\n", newline)
    encoding = "utf-8-sig" if bom else "utf-8"
    return normalized.encode(encoding)


def with_namespace(
    *,
    usings: list[str],
    namespace_style: str,
    class_lines: list[str],
    blank_after_namespace_open: bool,
    indent_class_body: bool,
) -> list[str]:
    lines = list(usings)
    lines.append("")
    if namespace_style == "file_scoped":
        lines.append("namespace IntlThrdPerfSchd;")
        lines.append("")
        lines.extend(class_lines)
        return lines

    lines.append("namespace IntlThrdPerfSchd")
    lines.append("{")
    if blank_after_namespace_open:
        lines.append("")

    if indent_class_body:
        lines.extend(("    " + line) if line else "" for line in class_lines)
    else:
        lines.extend(class_lines)
    lines.append("}")
    return lines


def matches_exact_line(lines: list[str], line_no: int, expected: str) -> bool:
    if line_no < 1 or line_no > len(lines):
        return False
    return lines[line_no - 1] == expected


def matches_stripped_line(lines: list[str], line_no: int, expected_values: set[str]) -> bool:
    if line_no < 1 or line_no > len(lines):
        return False
    return lines[line_no - 1].strip() in expected_values


def generate_projectinstaller_cs() -> Iterable[Candidate]:
    summary_blocks = {
        "none": [],
        "cn_project": [
            "        /// <summary>",
            "        /// ProjectInstaller 的摘要说明。",
            "        /// </summary>",
        ],
        "en_project": [
            "        /// <summary>",
            "        /// Summary description for ProjectInstaller.",
            "        /// </summary>",
        ],
    }

    handler_styles = {
        "multiline_empty": lambda name: [
            f"        private void {name}(object sender, InstallEventArgs e)",
            "        {",
            "        }",
        ],
        "single_line_empty": lambda name: [
            f"        private void {name}(object sender, InstallEventArgs e) {{ }}",
        ],
    }

    namespace_variants = [
        ("block_standard", True, True),
        ("block_standard", False, True),
        ("block_flush", True, False),
        ("file_scoped", False, True),
    ]

    for using_service_process, class_modifier, summary_name, init_with_this, gap1, gap2, handler_style_name, namespace_style, blank_after_open, indent_class_body in product(
        [False, True],
        ["public partial class", "public class", "partial class"],
        summary_blocks.keys(),
        [False, True],
        [1, 2],
        [1, 2],
        handler_styles.keys(),
        [item[0] for item in namespace_variants],
        [True, False],
        [True, False],
    ):
        if namespace_style == "file_scoped" and blank_after_open:
            continue
        if namespace_style == "block_flush" and indent_class_body:
            continue
        if namespace_style == "block_standard" and not indent_class_body:
            continue

        usings = [
            "using System.ComponentModel;",
            "using System.Configuration.Install;",
        ]
        if using_service_process:
            usings.append("using System.ServiceProcess;")

        init_call = "this.InitializeComponent();" if init_with_this else "InitializeComponent();"
        handler_factory = handler_styles[handler_style_name]

        class_lines = []
        class_lines.extend(summary_blocks[summary_name])
        class_lines.append("        [RunInstaller(true)]")
        class_lines.append(f"        {class_modifier} ProjectInstaller : Installer")
        class_lines.append("        {")
        class_lines.append("            public ProjectInstaller()")
        class_lines.append("            {")
        class_lines.append(f"                {init_call}")
        class_lines.append("            }")
        class_lines.extend("" for _ in range(gap1))
        class_lines.extend(handler_factory("serviceInstaller1_AfterInstall"))
        class_lines.extend("" for _ in range(gap2))
        class_lines.extend(handler_factory("serviceInstaller2_AfterInstall"))
        class_lines.append("        }")

        lines = with_namespace(
            usings=usings,
            namespace_style=namespace_style,
            class_lines=class_lines,
            blank_after_namespace_open=blank_after_open,
            indent_class_body=indent_class_body,
        )

        if not matches_stripped_line(lines, 14, {"public ProjectInstaller()"}):
            continue
        if not matches_stripped_line(lines, 16, {init_call}):
            continue
        if not matches_stripped_line(lines, 17, {"}"}):
            continue
        if not matches_stripped_line(
            lines,
            22,
            {
                "}",
                "private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e) { }",
            },
        ):
            continue
        if not matches_stripped_line(
            lines,
            27,
            {
                "}",
                "private void serviceInstaller2_AfterInstall(object sender, InstallEventArgs e) { }",
            },
        ):
            continue

        yield Candidate(
            file_name="ProjectInstaller.cs",
            variant_name="generated",
            text="\n".join(lines) + "\n",
            metadata={
                "using_service_process": str(using_service_process),
                "class_modifier": class_modifier,
                "summary_name": summary_name,
                "init_with_this": str(init_with_this),
                "gap1": str(gap1),
                "gap2": str(gap2),
                "handler_style": handler_style_name,
                "namespace_style": namespace_style,
                "blank_after_open": str(blank_after_open),
                "indent_class_body": str(indent_class_body),
            },
        )


def generate_service1_designer() -> Iterable[Candidate]:
    field_comment_blocks = {
        "none": [],
        "en_required": [
            "        /// <summary>",
            "        /// Required designer variable.",
            "        /// </summary>",
        ],
        "cn_required_space": [
            "        /// <summary> ",
            "        /// 必需的设计器变量。",
            "        /// </summary>",
        ],
        "cn_required": [
            "        /// <summary>",
            "        /// 设计器变量。",
            "        /// </summary>",
        ],
    }
    dispose_comment_blocks = {
        "en_clean": [
            "        /// <summary>",
            "        /// Clean up any resources being used.",
            "        /// </summary>",
        ],
        "cn_clean": [
            "        /// <summary>",
            "        /// 清理所有正在使用的资源。",
            "        /// </summary>",
        ],
        "cn_clean_param": [
            "        /// <summary>",
            "        /// 清理所有正在使用的资源。",
            "        /// </summary>",
            '        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>',
        ],
        "none": [],
    }
    init_comment_blocks = {
        "none": [],
        "en_required_4": [
            "        /// <summary>",
            "        /// Required method for Designer support - do not modify",
            "        /// the contents of this method with the code editor.",
            "        /// </summary>",
        ],
        "en_required_3": [
            "        /// <summary>",
            "        /// Required method for Designer support - do not modify.",
            "        /// </summary>",
        ],
        "cn_required_3": [
            "        /// <summary>",
            "        /// 设计器支持所需的方法 - 不要使用代码编辑器修改。",
            "        /// </summary>",
        ],
        "cn_template_4": [
            "        /// <summary> ",
            "        /// 设计器支持所需的方法 - 不要修改",
            "        /// 使用代码编辑器修改此方法的内容。",
            "        /// </summary>",
        ],
    }
    namespace_variants = [
        ("block_standard", False, True),
        ("block_standard", True, True),
        ("block_flush", True, False),
        ("file_scoped", False, True),
    ]

    for namespace_style, blank_after_open, indent_class_body, using_block, class_modifier, field_comment, dispose_comment, init_comment, field_decl, component_assignment_line, use_this_service_name, include_region, region_line, blank_after_region in product(
        [item[0] for item in namespace_variants],
        [False, True],
        [True, False],
        [[], ["using System.ComponentModel;"]],
        ["public partial class", "partial class"],
        field_comment_blocks.keys(),
        dispose_comment_blocks.keys(),
        init_comment_blocks.keys(),
        [
            "        private IContainer components;",
            "        private IContainer components = null;",
            "        private System.ComponentModel.IContainer components = null;",
            "        private System.ComponentModel.IContainer components;",
        ],
        [
            "            this.components = new System.ComponentModel.Container();",
            "            components = new System.ComponentModel.Container();",
        ],
        [False, True],
        [False, True],
        [
            "        #region Component Designer generated code",
            "        #region 组件设计器生成的代码",
        ],
        [False, True],
    ):
        if namespace_style == "file_scoped" and blank_after_open:
            continue
        if namespace_style == "block_flush" and indent_class_body:
            continue
        if namespace_style == "block_standard" and not indent_class_body:
            continue
        if not include_region and blank_after_region:
            continue

        service_name_line = (
            '            this.ServiceName = "Service1";'
            if use_this_service_name
            else '            base.ServiceName = "Service1";'
        )

        class_lines: list[str] = [
            f"        {class_modifier} Service1",
            "        {",
        ]
        class_lines.extend(field_comment_blocks[field_comment])
        class_lines.append(field_decl)
        class_lines.extend(dispose_comment_blocks[dispose_comment])
        class_lines.append("        protected override void Dispose(bool disposing)")
        class_lines.append("        {")
        class_lines.append("            if (disposing && (components != null))")
        class_lines.append("            {")
        class_lines.append("                components.Dispose();")
        class_lines.append("            }")
        class_lines.append("            base.Dispose(disposing);")
        class_lines.append("        }")
        class_lines.append("")
        if include_region:
            class_lines.append(region_line)
            if blank_after_region:
                class_lines.append("")
        class_lines.extend(init_comment_blocks[init_comment])
        class_lines.append("        private void InitializeComponent()")
        class_lines.append("        {")
        class_lines.append(component_assignment_line)
        class_lines.append(service_name_line)
        class_lines.append("        }")
        if include_region:
            class_lines.append("")
            class_lines.append("        #endregion")
        class_lines.append("        }")

        lines = with_namespace(
            usings=using_block,
            namespace_style=namespace_style,
            class_lines=class_lines,
            blank_after_namespace_open=blank_after_open,
            indent_class_body=indent_class_body,
        )

        if not matches_exact_line(lines, 31, component_assignment_line):
            continue
        if not matches_exact_line(lines, 32, service_name_line):
            continue
        if not matches_stripped_line(lines, 33, {"}"}):
            continue
        if not matches_exact_line(lines, 16, "            if (disposing && (components != null))"):
            continue
        if not matches_exact_line(lines, 18, "                components.Dispose();"):
            continue
        if not matches_exact_line(lines, 20, "            base.Dispose(disposing);"):
            continue

        yield Candidate(
            file_name="Service1.Designer.cs",
            variant_name="generated",
            text="\n".join(lines) + "\n",
            metadata={
                "namespace_style": namespace_style,
                "blank_after_open": str(blank_after_open),
                "indent_class_body": str(indent_class_body),
                "using_block": "|".join(using_block) if using_block else "(none)",
                "class_modifier": class_modifier,
                "field_comment": field_comment,
                "dispose_comment": dispose_comment,
                "init_comment": init_comment,
                "field_decl": field_decl,
                "component_assignment_line": component_assignment_line,
                "use_this_service_name": str(use_this_service_name),
                "include_region": str(include_region),
                "region_line": region_line,
                "blank_after_region": str(blank_after_region),
            },
        )


def generate_projectinstaller_designer() -> Iterable[Candidate]:
    field_comment_blocks = {
        "none": [],
        "en_required": [
            "        /// <summary>",
            "        /// Required designer variable.",
            "        /// </summary>",
        ],
        "cn_required_space": [
            "        /// <summary> ",
            "        /// 必需的设计器变量。",
            "        /// </summary>",
        ],
        "cn_required": [
            "        /// <summary>",
            "        /// 设计器变量。",
            "        /// </summary>",
        ],
    }
    dispose_comment_blocks = {
        "en_clean": [
            "        /// <summary>",
            "        /// Clean up any resources being used.",
            "        /// </summary>",
        ],
        "cn_clean": [
            "        /// <summary>",
            "        /// 清理所有正在使用的资源。",
            "        /// </summary>",
        ],
        "cn_clean_param_space": [
            "        /// <summary> ",
            "        /// 清理所有正在使用的资源。",
            "        /// </summary>",
            '        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>',
        ],
        "none": [],
    }
    init_comment_blocks = {
        "none": [],
        "en_required_4": [
            "        /// <summary>",
            "        /// Required method for Designer support - do not modify",
            "        /// the contents of this method with the code editor.",
            "        /// </summary>",
        ],
        "en_required_3": [
            "        /// <summary>",
            "        /// Required method for Designer support - do not modify.",
            "        /// </summary>",
        ],
        "cn_template_4": [
            "        /// <summary>",
            "        /// 设计器支持所需的方法 - 不要修改",
            "        /// 使用代码编辑器修改此方法的内容。",
            "        /// </summary>",
        ],
        "cn_template_4_space": [
            "        /// <summary> ",
            "        /// 设计器支持所需的方法 - 不要修改",
            "        /// 使用代码编辑器修改此方法的内容。",
            "        /// </summary>",
        ],
    }

    addrange_variants = {
        "this_fulltype": [
            "            this.Installers.AddRange(new System.Configuration.Install.Installer[] {",
            "                        this.serviceProcessInstaller1,",
            "                        this.serviceInstaller1});",
        ],
        "base_fulltype": [
            "            base.Installers.AddRange(new System.Configuration.Install.Installer[] {",
            "                        this.serviceProcessInstaller1,",
            "                        this.serviceInstaller1});",
        ],
        "this_shorttype": [
            "            this.Installers.AddRange(new Installer[] {",
            "                        this.serviceProcessInstaller1,",
            "                        this.serviceInstaller1});",
        ],
    }

    field_variants = {
        "simple": [
            "        private IContainer components;",
            "        private ServiceProcessInstaller serviceProcessInstaller1;",
            "        private ServiceInstaller serviceInstaller1;",
        ],
        "full_null_components": [
            "        private System.ComponentModel.IContainer components = null;",
            "        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller1;",
            "        private System.ServiceProcess.ServiceInstaller serviceInstaller1;",
        ],
    }

    namespace_variants = [
        ("block_standard", False, True),
        ("block_standard", True, True),
        ("block_flush", True, False),
        ("file_scoped", False, True),
    ]

    for namespace_style, blank_after_open, indent_class_body, using_block, class_modifier, field_variant, field_comment, dispose_comment, init_comment, include_region, region_line, blank_after_region, event_binding_style, addrange_style, insert_blank_between_fields in product(
        [item[0] for item in namespace_variants],
        [False, True],
        [True, False],
        [
            [],
            ["using System.ComponentModel;", "using System.Configuration.Install;", "using System.ServiceProcess;"],
        ],
        ["public partial class", "partial class"],
        field_variants.keys(),
        field_comment_blocks.keys(),
        dispose_comment_blocks.keys(),
        init_comment_blocks.keys(),
        [False, True],
        [
            "        #region Component Designer generated code",
            "        #region 组件设计器生成的代码",
        ],
        [False, True],
        ["explicit_full", "explicit_short", "method_group"],
        addrange_variants.keys(),
        [False, True],
    ):
        if namespace_style == "file_scoped" and blank_after_open:
            continue
        if namespace_style == "block_flush" and indent_class_body:
            continue
        if namespace_style == "block_standard" and not indent_class_body:
            continue
        if not include_region and blank_after_region:
            continue

        field_lines: list[str] = []
        field_lines.extend(field_comment_blocks[field_comment])
        for index, field_line in enumerate(field_variants[field_variant]):
            field_lines.append(field_line)
            if insert_blank_between_fields and index < len(field_variants[field_variant]) - 1:
                field_lines.append("")

        if event_binding_style == "explicit_full":
            after_install_line = (
                "            this.serviceInstaller1.AfterInstall += new "
                "System.Configuration.Install.InstallEventHandler(this.serviceInstaller1_AfterInstall);"
            )
        elif event_binding_style == "explicit_short":
            after_install_line = (
                "            this.serviceInstaller1.AfterInstall += new "
                "InstallEventHandler(this.serviceInstaller1_AfterInstall);"
            )
        else:
            after_install_line = "            this.serviceInstaller1.AfterInstall += this.serviceInstaller1_AfterInstall;"

        class_lines: list[str] = [
            f"        {class_modifier} ProjectInstaller",
            "        {",
        ]
        class_lines.extend(field_lines)
        class_lines.extend(dispose_comment_blocks[dispose_comment])
        class_lines.append("        protected override void Dispose(bool disposing)")
        class_lines.append("        {")
        class_lines.append("            if (disposing && (components != null))")
        class_lines.append("            {")
        class_lines.append("                components.Dispose();")
        class_lines.append("            }")
        class_lines.append("            base.Dispose(disposing);")
        class_lines.append("        }")
        class_lines.append("")
        if include_region:
            class_lines.append(region_line)
            if blank_after_region:
                class_lines.append("")
        class_lines.extend(init_comment_blocks[init_comment])
        class_lines.append("        private void InitializeComponent()")
        class_lines.append("        {")
        class_lines.append("            this.serviceProcessInstaller1 = new System.ServiceProcess.ServiceProcessInstaller();")
        class_lines.append("            this.serviceInstaller1 = new System.ServiceProcess.ServiceInstaller();")
        class_lines.append("            this.serviceProcessInstaller1.Account = System.ServiceProcess.ServiceAccount.LocalSystem;")
        class_lines.append("            this.serviceProcessInstaller1.Password = null;")
        class_lines.append("            this.serviceProcessInstaller1.Username = null;")
        class_lines.append("            this.serviceInstaller1.DelayedAutoStart = true;")
        class_lines.append('            this.serviceInstaller1.Description = "基于线程的调度器";')
        class_lines.append('            this.serviceInstaller1.DisplayName = "IntlThrdSchd";')
        class_lines.append('            this.serviceInstaller1.ServiceName = "IntlThrdSchd";')
        class_lines.append("            this.serviceInstaller1.StartType = ServiceStartMode.Automatic;")
        class_lines.append(after_install_line)
        class_lines.extend(addrange_variants[addrange_style])
        class_lines.append("        }")
        if include_region:
            class_lines.append("")
            class_lines.append("        #endregion")
        class_lines.append("        }")

        lines = with_namespace(
            usings=using_block,
            namespace_style=namespace_style,
            class_lines=class_lines,
            blank_after_namespace_open=blank_after_open,
            indent_class_body=indent_class_body,
        )

        required = {
            31: "            this.serviceProcessInstaller1 = new System.ServiceProcess.ServiceProcessInstaller();",
            32: "            this.serviceInstaller1 = new System.ServiceProcess.ServiceInstaller();",
            36: "            this.serviceProcessInstaller1.Account = System.ServiceProcess.ServiceAccount.LocalSystem;",
            37: "            this.serviceProcessInstaller1.Password = null;",
            38: "            this.serviceProcessInstaller1.Username = null;",
            42: "            this.serviceInstaller1.DelayedAutoStart = true;",
            43: '            this.serviceInstaller1.Description = "基于线程的调度器";',
            44: '            this.serviceInstaller1.DisplayName = "IntlThrdSchd";',
            45: '            this.serviceInstaller1.ServiceName = "IntlThrdSchd";',
            46: "            this.serviceInstaller1.StartType = ServiceStartMode.Automatic;",
            47: after_install_line,
            51: addrange_variants[addrange_style][0],
            52: addrange_variants[addrange_style][1],
            53: addrange_variants[addrange_style][2],
        }
        if any(not matches_exact_line(lines, line_no, expected) for line_no, expected in required.items()):
            continue
        if not matches_stripped_line(lines, 55, {"}"}):
            continue

        yield Candidate(
            file_name="ProjectInstaller.Designer.cs",
            variant_name="generated",
            text="\n".join(lines) + "\n",
            metadata={
                "namespace_style": namespace_style,
                "blank_after_open": str(blank_after_open),
                "indent_class_body": str(indent_class_body),
                "using_block": "|".join(using_block) if using_block else "(none)",
                "class_modifier": class_modifier,
                "field_variant": field_variant,
                "field_comment": field_comment,
                "dispose_comment": dispose_comment,
                "init_comment": init_comment,
                "include_region": str(include_region),
                "region_line": region_line,
                "blank_after_region": str(blank_after_region),
                "event_binding_style": event_binding_style,
                "addrange_style": addrange_style,
                "insert_blank_between_fields": str(insert_blank_between_fields),
            },
        )


def current_file_candidates(root: Path) -> Iterable[Candidate]:
    for name in TARGETS:
        path = root / name
        if path.exists():
            yield Candidate(
                file_name=name,
                variant_name="existing",
                text=path.read_text(encoding="utf-8", errors="replace"),
                metadata={"source": str(path)},
            )


def reference_file_candidates(root: Path) -> Iterable[Candidate]:
    for name in TARGETS:
        path = root / name
        if path.exists():
            yield Candidate(
                file_name=name,
                variant_name="reference",
                text=path.read_text(encoding="utf-8", errors="replace"),
                metadata={"source": str(path)},
            )


def search_candidates() -> dict[str, dict[str, object]]:
    results: dict[str, dict[str, object]] = {
        name: {"target_checksum": checksum, "tested_candidate_count": 0, "hit": None}
        for name, checksum in TARGETS.items()
    }

    generators = [
        current_file_candidates(Path("recovered_src_2.35/IntlThrdPerfSchd")),
        current_file_candidates(Path("recovered_src_2.36/IntlThrdPerfSchd")),
        reference_file_candidates(Path("2.34版逆向理论资料/pdb_aligned_src/IntlThrdPerfSchd")),
        generate_projectinstaller_cs(),
        generate_projectinstaller_designer(),
        generate_service1_designer(),
    ]

    for source in generators:
        for candidate in source:
            result = results[candidate.file_name]
            if result["hit"] is not None:
                continue
            for bom, newline in product([False, True], ["\n", "\r\n"]):
                data = encode_text(candidate.text, newline=newline, bom=bom)
                checksum = hashlib.sha256(data).hexdigest()
                result["tested_candidate_count"] += 1
                if checksum != result["target_checksum"]:
                    continue
                result["hit"] = {
                    "variant_name": candidate.variant_name,
                    "metadata": {**candidate.metadata, "bom": bom, "newline": "CRLF" if newline == "\r\n" else "LF"},
                    "sha256": checksum,
                    "text": candidate.text.replace("\n", newline),
                }
                break

    return results


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Search constrained checksum candidates for small designer/code-behind files."
    )
    parser.add_argument("--output", required=True, help="Path to write JSON summary.")
    args = parser.parse_args()

    results = search_candidates()
    payload = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "targets": results,
    }

    output_path = Path(args.output).resolve()
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
