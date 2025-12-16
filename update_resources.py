import argparse
import re
import sys
from pathlib import Path


def get_resx_keys(path: Path) -> set[str]:
    content = path.read_text(encoding="utf-8")
    return set(re.findall(r'<data name="([^"]+)"', content))


def get_designer_keys(path: Path) -> set[str]:
    content = path.read_text(encoding="utf-8")
    return set(re.findall(r"public static string ([^\s]+) {", content))


def update_designer(repo_root: Path) -> None:
    resx_path = repo_root / "src" / "NAVIGEST.Shared" / "Resources" / "Strings" / "AppResources.resx"
    designer_path = repo_root / "src" / "NAVIGEST.Shared" / "Resources" / "Strings" / "AppResources.Designer.cs"

    for path in (resx_path, designer_path):
        if not path.exists():
            print(f"Erro: ficheiro não encontrado: {path}", file=sys.stderr)
            sys.exit(1)

    resx_keys = get_resx_keys(resx_path)
    designer_keys = get_designer_keys(designer_path)

    missing_keys = resx_keys - designer_keys

    if not missing_keys:
        print("No missing keys found.")
        return

    print(f"Found {len(missing_keys)} missing keys.")

    new_properties = []
    for key in sorted(missing_keys):
        prop = f"""
        public static string {key} {{
            get {{
                return ResourceManager.GetString("{key}", resourceCulture);
            }}
        }}"""
        new_properties.append(prop)

    content = designer_path.read_text(encoding="utf-8")

    last_brace_index = content.rfind('}')
    second_last_brace_index = content.rfind('}', 0, last_brace_index)

    if second_last_brace_index == -1:
        print("Could not find insertion point.", file=sys.stderr)
        sys.exit(1)

    new_content = (
        content[:second_last_brace_index]
        + "\n"
        + "\n".join(new_properties)
        + "\n    "
        + content[second_last_brace_index:]
    )

    designer_path.write_text(new_content, encoding="utf-8")
    print("Updated AppResources.Designer.cs")


def main() -> None:
    parser = argparse.ArgumentParser(description="Update AppResources.Designer.cs with missing RESX keys")
    parser.add_argument("--repo-root", type=Path, help="Caminho para a raiz do repositório (opcional)")
    args = parser.parse_args()

    repo_root = args.repo_root.expanduser().resolve() if args.repo_root else Path(__file__).resolve().parent
    update_designer(repo_root)


if __name__ == "__main__":
    main()
