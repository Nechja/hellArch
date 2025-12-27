"""HellArch Data Types Code Generator

Generates typed data models from JSON Schema for:
- Go (structs)
- Rust (structs with serde)
- Python (dataclasses)
- C# (records)
- F# (records)
"""

import json
import subprocess
from dataclasses import dataclass
from pathlib import Path
from typing import Any


@dataclass
class Schema:
    path: Path
    domain: str
    name: str
    data: dict[str, Any]


def load_schemas(schemas_dir: Path) -> list[Schema]:
    """Load all JSON schemas from the schemas directory."""
    schemas = []
    for domain_dir in schemas_dir.iterdir():
        if not domain_dir.is_dir():
            continue
        domain = domain_dir.name
        for schema_file in domain_dir.glob("*.json"):
            with open(schema_file) as f:
                data = json.load(f)
            schemas.append(Schema(
                path=schema_file,
                domain=domain,
                name=schema_file.stem,
                data=data
            ))
    return schemas


def generate_go(schema: Schema, output_dir: Path) -> bool:
    """Generate Go struct using quicktype."""
    out_file = output_dir / schema.domain / f"{schema.name}.go"
    out_file.parent.mkdir(parents=True, exist_ok=True)

    result = subprocess.run([
        "quicktype", "-s", "schema", str(schema.path),
        "-o", str(out_file),
        "--package", schema.domain,
        "--just-types"
    ], capture_output=True)

    return result.returncode == 0


def generate_rust(schema: Schema, output_dir: Path) -> bool:
    """Generate Rust struct using quicktype."""
    out_file = output_dir / schema.domain / f"{schema.name}.rs"
    out_file.parent.mkdir(parents=True, exist_ok=True)

    result = subprocess.run([
        "quicktype", "-s", "schema", str(schema.path),
        "-o", str(out_file),
        "--derive-debug",
        "--visibility", "public"
    ], capture_output=True)

    return result.returncode == 0


def generate_python(schema: Schema, output_dir: Path) -> bool:
    """Generate Python dataclass using quicktype."""
    out_file = output_dir / schema.domain / f"{schema.name}.py"
    out_file.parent.mkdir(parents=True, exist_ok=True)

    result = subprocess.run([
        "quicktype", "-s", "schema", str(schema.path),
        "-o", str(out_file),
        "--just-types"
    ], capture_output=True)

    return result.returncode == 0


def generate_csharp(schema: Schema, output_dir: Path) -> bool:
    """Generate C# record using quicktype."""
    out_file = output_dir / schema.domain.title() / f"{schema.name}.cs"
    out_file.parent.mkdir(parents=True, exist_ok=True)

    namespace = f"HellArch.DataTypes.{schema.domain.title()}"

    result = subprocess.run([
        "quicktype", "-s", "schema", str(schema.path),
        "-o", str(out_file),
        "--namespace", namespace,
        "--features", "just-types"
    ], capture_output=True)

    return result.returncode == 0


def generate_fsharp(schema: Schema, output_dir: Path) -> bool:
    """Generate F# record (custom generator)."""
    out_file = output_dir / schema.domain.title() / f"{schema.name}.fs"
    out_file.parent.mkdir(parents=True, exist_ok=True)

    title = schema.data.get("title", "Unknown")
    props = schema.data.get("properties", {})
    required = set(schema.data.get("required", []))
    defs = schema.data.get("$defs", {})

    module_name = schema.domain.title()

    lines = [
        f"namespace HellArch.DataTypes.{module_name}",
        "",
        "open System",
        ""
    ]

    # Generate enums from $defs
    for def_name, def_schema in defs.items():
        if "enum" in def_schema:
            lines.append(f"type {def_name} =")
            for val in def_schema["enum"]:
                pascal_val = to_pascal(val) if val else "Unknown"
                lines.append(f"    | {pascal_val}")
            lines.append("")

    # Generate main record
    lines.append(f"type {title} = {{")
    for prop_name, prop_schema in props.items():
        pascal_name = to_pascal(prop_name)
        fs_type = json_to_fsharp_type(prop_schema, prop_name in required)
        lines.append(f"    {pascal_name}: {fs_type}")
    lines.append("}")
    lines.append("")

    out_file.write_text("\n".join(lines))
    return True


def to_pascal(s: str) -> str:
    """Convert snake_case to PascalCase."""
    return ''.join(word.title() for word in s.split('_'))


def json_to_fsharp_type(prop_schema: dict, is_required: bool) -> str:
    """Convert JSON Schema type to F# type."""
    t = prop_schema.get("type", "string")
    fmt = prop_schema.get("format", "")

    # Handle nullable types
    if isinstance(t, list):
        t = next((x for x in t if x != "null"), "string")
        is_required = False

    # Handle refs/enums
    if "$ref" in prop_schema:
        ref = prop_schema["$ref"]
        base = ref.split("/")[-1]
    elif "enum" in prop_schema:
        base = "string"
    elif t == "string":
        if fmt == "uuid":
            base = "Guid"
        elif fmt == "date-time":
            base = "DateTime"
        else:
            base = "string"
    elif t == "integer":
        base = "int"
    elif t == "boolean":
        base = "bool"
    elif t == "number":
        base = "float"
    else:
        base = "obj"

    return base if is_required else f"{base} option"


def main():
    """Run the code generator."""
    import argparse

    parser = argparse.ArgumentParser(description="Generate typed data models from JSON Schema")
    parser.add_argument("--schemas", "-s", type=Path, default=Path("schemas"),
                        help="Path to schemas directory")
    parser.add_argument("--output", "-o", type=Path, default=Path("."),
                        help="Output root directory")
    parser.add_argument("--lang", "-l", nargs="+",
                        choices=["go", "rust", "python", "csharp", "fsharp", "all"],
                        default=["all"],
                        help="Languages to generate")
    args = parser.parse_args()

    if "all" in args.lang:
        langs = ["go", "rust", "python", "csharp", "fsharp"]
    else:
        langs = args.lang

    generators = {
        "go": generate_go,
        "rust": generate_rust,
        "python": generate_python,
        "csharp": generate_csharp,
        "fsharp": generate_fsharp,
    }

    schemas = load_schemas(args.schemas)
    print(f"Found {len(schemas)} schemas")

    for schema in schemas:
        print(f"  {schema.domain}/{schema.name}")
        for lang in langs:
            output_dir = args.output / lang
            success = generators[lang](schema, output_dir)
            status = "ok" if success else "failed"
            print(f"    {lang}: {status}")

    print("\nDone!")


if __name__ == "__main__":
    main()
