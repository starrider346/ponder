DESCRIPTION = (
    "Package only Ponder's DLL and mod metadata, following the sibling mods' layout."
)
import argparse
import subprocess
from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile

ROOT = Path(__file__).resolve().parent.parent


def main():
    parser = argparse.ArgumentParser(description=DESCRIPTION)
    parser.add_argument("--build", action="store_true")
    parser.add_argument("--dll", type=Path, default=ROOT / "Releases/Ponder.dll")
    parser.add_argument("--output", type=Path, default=ROOT / "Releases/Ponder.zip")
    args = parser.parse_args()
    if args.build:
        subprocess.run(
            ["dotnet", "build", "Ponder.slnx", "-c", "Release", "-p:SD_MODS_DIR="],
            cwd=ROOT,
            check=True,
        )
    if not args.dll.is_file():
        parser.error(f"DLL missing: {args.dll}; build Release first or use --build")
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with ZipFile(args.output, "w", ZIP_DEFLATED) as archive:
        archive.write(args.dll, "Mods/Ponder.dll")
        archive.write(ROOT / "mod.json", "mod.json")
    print(args.output)


if __name__ == "__main__":
    main()
